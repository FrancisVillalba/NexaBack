using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;

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

    public Task<bool> ExisteNombreUsuarioAsync(string value, CancellationToken cancellationToken = default)
        => context.Usuarios.AnyAsync(x => x.NombreUsuario == value, cancellationToken);

    public Task<bool> ExistePersonaAsync(int personaId, CancellationToken cancellationToken = default)
        => context.Usuarios.AnyAsync(x => x.PersonaId == personaId, cancellationToken);

    public async Task AgregarAsync(Usuario entity, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }
}

