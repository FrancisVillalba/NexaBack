using NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
namespace NexaBack.Repository.CuentasProveedores;
public interface ICuentaProveedorRepository
{
    Task<CuentaProveedorDto> ObtenerAsync(int proveedorId, int paginaCompras, int paginaPagos, int tamanoPagina, CancellationToken ct);
}
