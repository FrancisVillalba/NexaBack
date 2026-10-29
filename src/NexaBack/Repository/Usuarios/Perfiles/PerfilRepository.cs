using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Usuarios.Perfiles;

public sealed class PerfilRepository(UsuariosDbContext context) : IPerfilRepository
{
    public async Task<IReadOnlyList<Perfil>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default)
        => await context.Perfiles.AsNoTracking().OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);

    public Task<Perfil?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => context.Perfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExisteNombreAsync(string value, CancellationToken cancellationToken = default)
        => context.Perfiles.AnyAsync(x => x.Nombre == value, cancellationToken);

    public async Task AgregarAsync(Perfil entity, CancellationToken cancellationToken = default)
    {
        context.Perfiles.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }
}

