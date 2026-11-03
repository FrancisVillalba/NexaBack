using NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
namespace NexaBack.Services.CuentasProveedores;
public interface ICuentaProveedorService
{
    Task<CuentaProveedorDto> ObtenerAsync(int proveedorId, int paginaCompras, int paginaPagos, int tamanoPagina, CancellationToken ct);
}
