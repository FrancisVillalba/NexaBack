using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
namespace NexaBack.Repository.Categorias;
public sealed class CategoriaRepository(UsuariosDbContext context) : ICategoriaRepository
{
    public async Task<(IReadOnlyList<Categoria> Items, int Total)> ListarAsync(int offset, int limite, CancellationToken ct)
    {
        var query = context.Categorias.AsNoTracking();
        return (await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), await query.CountAsync(ct));
    }
    public Task<Categoria?> ObtenerAsync(int id, CancellationToken ct)
        => context.Categorias.SingleOrDefaultAsync(x => x.Id == id, ct);
}
