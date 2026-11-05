using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Productos;

public sealed class ProductoRepository(UsuariosDbContext context) : IProductoRepository
{
    public async Task<(IReadOnlyList<Producto> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct)
    {
        var query = context.Productos.AsNoTracking().Include(x => x.Marca).Include(x => x.CategoriaCatalogo).Include(x => x.UnidadMedidaCatalogo).AsQueryable();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            // Escapar comodines permite buscar literalmente símbolos como % y _.
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Codigo, patron, "\\")
                || EF.Functions.ILike(x.Nombre, patron, "\\") || EF.Functions.ILike(x.CategoriaCatalogo.Nombre, patron, "\\")
                || EF.Functions.ILike(x.Marca == null ? "" : x.Marca.Nombre, patron, "\\") || EF.Functions.ILike(x.CodigoBarras ?? "", patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct);
        return (items, total);
    }

    public Task<Producto?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct)
        => (paraActualizar ? context.Productos : context.Productos.AsNoTracking()).Include(x => x.Marca).Include(x => x.CategoriaCatalogo).Include(x => x.UnidadMedidaCatalogo).SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId, CancellationToken ct)
        => context.Productos.AnyAsync(x => x.Codigo == codigo && x.Id != excluirId, ct);

    public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId, CancellationToken ct)
        => context.Productos.AnyAsync(x => x.CodigoBarras == codigoBarras && x.Id != excluirId, ct);

    public async Task AgregarAsync(Producto producto, CancellationToken ct)
    {
        context.Productos.Add(producto);
        await GuardarCambiosAsync(ct);
    }

    public Task<bool> TieneMovimientosAsync(int productoId, CancellationToken ct)
        => context.MovimientosInventario.AnyAsync(x => x.ProductoId == productoId, ct);

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try { await PersistenceErrors.SaveAsync(context, ct); }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("El producto cambió mientras lo editabas. Actualizá el listado y volvé a intentar.", 409);
        }
    }
}
