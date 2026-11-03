using NexaBack.Infrastructure.Data.Dtos.Pagos;
namespace NexaBack.Repository.Pagos;
public interface IPagoRepository
{
    Task<CompraPagosDto> ListarAsync(int compraId, int pagina, int tamanoPagina, CancellationToken ct);
    Task<PagoDto> RegistrarAsync(int compraId, RegistrarPagoRequest request, string hash, CancellationToken ct);
    Task<PagoDto> AnularAsync(int compraId, int pagoId, AnularPagoRequest request, CancellationToken ct);
}
