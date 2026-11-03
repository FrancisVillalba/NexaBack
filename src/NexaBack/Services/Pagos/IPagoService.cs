using NexaBack.Infrastructure.Data.Dtos.Pagos;
namespace NexaBack.Services.Pagos;
public interface IPagoService
{
    Task<CompraPagosDto> ListarAsync(int compraId, int pagina, int tamanoPagina, CancellationToken ct);
    Task<PagoDto> RegistrarAsync(int compraId, RegistrarPagoRequest request, CancellationToken ct);
    Task<PagoDto> AnularAsync(int compraId, int pagoId, AnularPagoRequest request, CancellationToken ct);
}
