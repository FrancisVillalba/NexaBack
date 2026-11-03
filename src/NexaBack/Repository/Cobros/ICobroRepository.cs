using NexaBack.Infrastructure.Data.Dtos.Cobros;
namespace NexaBack.Repository.Cobros;
public interface ICobroRepository
{
    Task<VentaCobrosDto> ListarAsync(int ventaId, int pagina, int tamanoPagina, CancellationToken ct);
    Task<CobroDto> RegistrarAsync(int ventaId, RegistrarCobroRequest request, string hash, CancellationToken ct);
    Task<CobroDto> AnularAsync(int ventaId, int cobroId, AnularCobroRequest request, CancellationToken ct);
}
