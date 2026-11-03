using NexaBack.Infrastructure.Data.Dtos.Cobros;
namespace NexaBack.Services.Cobros;
public interface ICobroService
{
    Task<VentaCobrosDto> ListarAsync(int ventaId, int pagina, int tamanoPagina, CancellationToken ct);
    Task<CobroDto> RegistrarAsync(int ventaId, RegistrarCobroRequest request, CancellationToken ct);
    Task<CobroDto> AnularAsync(int ventaId, int cobroId, AnularCobroRequest request, CancellationToken ct);
}
