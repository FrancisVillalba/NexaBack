using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Usuarios.Perfiles;

public sealed class PerfilRepository(UsuariosDbContext context) : IPerfilRepository
{
    public async Task<IReadOnlyList<Perfil>> ListarAsync(int offset, int limit,
        string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default)
    {
        // Los filtros se ejecutan en PostgreSQL antes de paginar, como en Personas.
        var query = context.Perfiles.AsNoTracking();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, patron, "\\")
                || EF.Functions.ILike(x.Descripcion ?? "", patron, "\\"));
        }
        return await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<Perfil?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => context.Perfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Perfil?> ObtenerParaActualizarAsync(int id, CancellationToken cancellationToken = default)
        // EF registra los cambios de esta entidad; los GET siguen sin seguimiento.
        => context.Perfiles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExisteNombreAsync(string value, CancellationToken cancellationToken = default, int? excluirId = null)
        => context.Perfiles.AnyAsync(x => x.Nombre == value && x.Id != excluirId, cancellationToken);

    public async Task AgregarAsync(Perfil entity, CancellationToken cancellationToken = default)
    {
        context.Perfiles.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => PersistenceErrors.SaveAsync(context, cancellationToken);
}

