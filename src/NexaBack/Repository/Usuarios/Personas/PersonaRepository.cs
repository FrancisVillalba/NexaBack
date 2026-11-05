using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;
using System.Data;
using NexaBack.Common;
using Npgsql;

namespace NexaBack.Repository.Usuarios.Personas;

public sealed class PersonaRepository(UsuariosDbContext context) : IPersonaRepository
{
    public async Task<IReadOnlyList<Persona>> ListarAsync(int offset, int limit,
        string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default)
    {
        // IQueryable arma una consulta SQL: se filtra y pagina en PostgreSQL, no en memoria.
        var query = context.Personas.AsNoTracking().Include(x => x.TipoDocumento).AsQueryable();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            // Escapar comodines permite buscar literalmente documentos que contengan % o _.
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Nombres + " " + x.Apellidos, patron, "\\")
                || EF.Functions.ILike(x.Documento ?? "", patron, "\\")
                || EF.Functions.ILike(x.Correo ?? "", patron, "\\"));
        }
        return await query.OrderBy(x => x.Nombres).ThenBy(x => x.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<Persona?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => context.Personas.AsNoTracking().Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Persona?> ObtenerParaActualizarAsync(int id, CancellationToken cancellationToken = default)
        // Sin AsNoTracking, EF detecta qué propiedades cambiaron al guardar.
        => context.Personas.Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExisteDocumentoAsync(int tipoDocumentoId, string value, CancellationToken cancellationToken = default, int? excluirId = null)
        => context.Personas.AnyAsync(x => x.Documento == value && (x.TipoDocumentoId == tipoDocumentoId || x.TipoDocumentoId == null) && x.Id != excluirId, cancellationToken);

    public async Task AgregarAsync(Persona entity, CancellationToken cancellationToken = default)
    {
        context.Personas.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => PersistenceErrors.SaveAsync(context, cancellationToken);

    public async Task<CambioEstadoPersona> CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        // La comprobación y la baja forman una transacción. Serializable evita que dos
        // solicitudes desactiven simultáneamente a los dos últimos administradores.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var persona = await ObtenerParaActualizarAsync(id, cancellationToken);
            if (persona is null) return CambioEstadoPersona.NoEncontrada;

            if (!activo && persona.Activo)
            {
                var administradores = context.Usuarios.Where(x => x.Activo && x.Persona.Activo
                    && x.Perfil.Activo && x.Perfil.Nombre == "Administrador");
                if (await administradores.AnyAsync(x => x.PersonaId == id, cancellationToken)
                    && !await administradores.AnyAsync(x => x.PersonaId != id, cancellationToken))
                    return CambioEstadoPersona.UltimoAdministrador;
            }

            persona.Activo = activo;
            await GuardarCambiosAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CambioEstadoPersona.Actualizada;
        }
        catch (Exception exception) when (exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
            || exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } })
        {
            throw new BusinessException("Otra operación modificó las personas. Actualizá el listado y volvé a intentar.", 409);
        }
    }
}

