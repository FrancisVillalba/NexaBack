using NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
namespace NexaBack.Repository.CuentasCorrientes;
public interface ICuentaCorrienteRepository
{
    Task<CuentaCorrienteDto> ObtenerAsync(int clienteId, int paginaVentas, int paginaCobros, int tamanoPagina, CancellationToken ct);
}
