using NexaBack.Infrastructure.Data.Dtos.Ventas;
namespace NexaBack.Services.Ventas;
public interface IVentaService
{
    Task<VentasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct);
    Task<VentaDto> ObtenerAsync(int id, CancellationToken ct);
    Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct);
    Task<VentaDto> ConfirmarAsync(VentaConfirmarRequest request, CancellationToken ct);
    Task<VentaDto> AnularAsync(int id, VentaAnularRequest request, CancellationToken ct);
    Task<IReadOnlyList<VentaClienteDto>> ClientesAsync(string buscar, CancellationToken ct);
    Task<IReadOnlyList<VentaProductoDto>> ProductosAsync(int depositoId, string buscar, CancellationToken ct);
    Task<IReadOnlyList<VentaDepositoDto>> DepositosAsync(CancellationToken ct);
}
