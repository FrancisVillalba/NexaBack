using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;
using NexaBack.Common;
using Npgsql;
using System.Data;

namespace NexaBack.Repository.Usuarios.Usuarios;

public sealed class UsuarioRepository(UsuariosDbContext context) : IUsuarioRepository
{
    public async Task<IReadOnlyList<Usuario>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default)
        => await context.Usuarios.AsNoTracking().OrderBy(x => x.NombreUsuario).ThenBy(x => x.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);

    public Task<Usuario?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => context.Usuarios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerConRelacionesAsync(int id, CancellationToken cancellationToken = default)
        => context.Usuarios.AsNoTracking().Include(x => x.Persona).Include(x => x.Perfil)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerPorNombreConRelacionesAsync(string nombre, CancellationToken cancellationToken = default)
        => context.Usuarios.AsNoTracking().Include(x => x.Persona).Include(x => x.Perfil)
            .SingleOrDefaultAsync(x => x.NombreUsuario == nombre, cancellationToken);

    public async Task<bool> ActualizarPasswordHashAsync(int id, string hashAnterior, string hashNuevo, CancellationToken cancellationToken = default)
        => await context.Usuarios.Where(x => x.Id == id && x.PasswordHash == hashAnterior)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.PasswordHash, hashNuevo), cancellationToken) == 1;

    public Task<bool> ExisteNombreUsuarioAsync(string value, CancellationToken cancellationToken = default, int? excluirId = null)
        => context.Usuarios.AnyAsync(x => x.NombreUsuario == value && x.Id != excluirId, cancellationToken);

    public Task<bool> ExistePersonaAsync(int personaId, CancellationToken cancellationToken = default)
        => context.Usuarios.AnyAsync(x => x.PersonaId == personaId, cancellationToken);

    public async Task AgregarAsync(Usuario entity, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }

    public Task<Usuario> ActualizarAsync(int id, string nombre, int perfilId, CancellationToken cancellationToken = default)
        => GuardarCambioProtegidoAsync(id, async usuario =>
        {
            var perfil = await context.Perfiles.SingleOrDefaultAsync(x => x.Id == perfilId, cancellationToken)
                ?? throw new BusinessException("El perfil no existe.", 404);
            if (!usuario.Persona.Activo || !perfil.Activo)
                throw new BusinessException("La persona y el perfil deben estar activos para editar la cuenta.");
            usuario.NombreUsuario = nombre;
            usuario.Perfil = perfil;
            usuario.PerfilId = perfil.Id;
        }, cancellationToken);

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default)
        => await GuardarCambioProtegidoAsync(id, usuario =>
        {
            if (activo && (!usuario.Persona.Activo || !usuario.Perfil.Activo))
                throw new BusinessException("No podés activar la cuenta: su persona y su perfil deben estar activos.");
            usuario.Activo = activo;
            return Task.CompletedTask;
        }, cancellationToken);

    private async Task<Usuario> GuardarCambioProtegidoAsync(int id, Func<Usuario, Task> cambiar,
        CancellationToken cancellationToken)
    {
        // La comprobación del último administrador y la escritura deben ser atómicas.
        // Serializable protege también frente a bajas simultáneas desde Personas.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var usuario = await context.Usuarios.Include(x => x.Persona).Include(x => x.Perfil)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new BusinessException("Usuario no encontrado.", 404);
            var eraAdministrador = EsAdministradorActivo(usuario);
            await cambiar(usuario);
            if (eraAdministrador && !EsAdministradorActivo(usuario)
                && !await context.Usuarios.AnyAsync(x => x.Id != id && x.Activo && x.Persona.Activo
                    && x.Perfil.Activo && x.Perfil.Nombre == "Administrador", cancellationToken))
                throw new BusinessException("No podés desactivar ni cambiar el perfil del último administrador activo.", 409);
            await PersistenceErrors.SaveAsync(context, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return usuario;
        }
        catch (Exception exception) when (exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
            || exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } })
        {
            throw new BusinessException("Otra operación modificó los usuarios. Actualizá el listado y volvé a intentar.", 409);
        }
    }

    private static bool EsAdministradorActivo(Usuario usuario)
        => usuario.Activo && usuario.Persona.Activo && usuario.Perfil.Activo && usuario.Perfil.Nombre == "Administrador";
}

