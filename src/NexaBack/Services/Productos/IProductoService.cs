using NexaBack.Infrastructure.Data.Dtos.Productos;

namespace NexaBack.Services.Productos;

public interface IProductoService
{
    Task<ProductosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct);
    Task<ProductoDto> ObtenerAsync(int id, CancellationToken ct);
    Task<ProductoDto> CrearAsync(GuardarProductoRequest request, CancellationToken ct);
    Task<ProductoDto> ActualizarAsync(int id, GuardarProductoRequest request, CancellationToken ct);
    Task CambiarEstadoAsync(int id, CambiarEstadoProductoRequest request, CancellationToken ct);
}
