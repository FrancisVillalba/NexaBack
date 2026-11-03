using NexaBack.Infrastructure.Data.Dtos.Compras;
namespace NexaBack.Services.Compras;

public interface ICompraService
{
    Task<ComprasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct);
    Task<CompraDto> ObtenerAsync(int id, CancellationToken ct);
    Task<CompraDto> CrearAsync(CrearCompraRequest request, CancellationToken ct);
    Task<CompraDto> RecibirAsync(int id, CompraEstadoRequest request, CancellationToken ct);
    Task<CompraDto> CancelarAsync(int id, CompraEstadoRequest request, CancellationToken ct);
    Task<IReadOnlyList<CompraProveedorDto>> ProveedoresAsync(string buscar, CancellationToken ct);
    Task<IReadOnlyList<CompraProductoDto>> ProductosAsync(string buscar, CancellationToken ct);
    Task<IReadOnlyList<CompraDepositoDto>> DepositosAsync(CancellationToken ct);
}
