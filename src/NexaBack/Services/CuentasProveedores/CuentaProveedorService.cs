using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
using NexaBack.Repository.CuentasProveedores;
namespace NexaBack.Services.CuentasProveedores;
public sealed class CuentaProveedorService(ICuentaProveedorRepository repository) : ICuentaProveedorService
{
    public Task<CuentaProveedorDto> ObtenerAsync(int proveedorId, int paginaCompras, int paginaPagos, int tamanoPagina, CancellationToken ct)
    {
        if (proveedorId <= 0) throw new BusinessException("Proveedor inválido.");
        var size = Validation.PageSize(tamanoPagina);
        Validation.Offset(paginaCompras, size); Validation.Offset(paginaPagos, size);
        return repository.ObtenerAsync(proveedorId, paginaCompras, paginaPagos, size, ct);
    }
}
