using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
namespace NexaBack.Repository.UnidadesMedida;
public sealed class UnidadMedidaRepository(UsuariosDbContext context) : IUnidadMedidaRepository
{
    public async Task<(IReadOnlyList<UnidadMedida> Items, int Total)> ListarAsync(int offset, int limite, CancellationToken ct)
    {
        var query = context.UnidadesMedida.AsNoTracking();
        return (await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), await query.CountAsync(ct));
    }
    public Task<UnidadMedida?> ObtenerAsync(int id, CancellationToken ct)
        => context.UnidadesMedida.SingleOrDefaultAsync(x => x.Id == id, ct);
}
