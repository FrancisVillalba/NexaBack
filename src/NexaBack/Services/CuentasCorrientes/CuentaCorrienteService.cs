using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
using NexaBack.Repository.CuentasCorrientes;
namespace NexaBack.Services.CuentasCorrientes;
public sealed class CuentaCorrienteService(ICuentaCorrienteRepository repository) : ICuentaCorrienteService
{
    public Task<CuentaCorrienteDto> ObtenerAsync(int clienteId, int paginaVentas, int paginaCobros, int tamanoPagina, CancellationToken ct)
    {
        if (clienteId <= 0) throw new BusinessException("Cliente inválido.");
        var size = Validation.PageSize(tamanoPagina);
        Validation.Offset(paginaVentas, size); Validation.Offset(paginaCobros, size);
        return repository.ObtenerAsync(clienteId, paginaVentas, paginaCobros, size, ct);
    }
}
