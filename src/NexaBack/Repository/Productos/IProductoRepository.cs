using NexaBack.Infrastructure.Data.Context.Productos.Entity;

namespace NexaBack.Repository.Productos;

public interface IProductoRepository
{
    Task<(IReadOnlyList<Producto> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Producto?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId, CancellationToken ct);
    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId, CancellationToken ct);
    Task<bool> TieneMovimientosAsync(int productoId, CancellationToken ct);
    Task AgregarAsync(Producto producto, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
}
