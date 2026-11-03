using NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
namespace NexaBack.Services.CuentasCorrientes;
public interface ICuentaCorrienteService
{
    Task<CuentaCorrienteDto> ObtenerAsync(int clienteId, int paginaVentas, int paginaCobros, int tamanoPagina, CancellationToken ct);
}
