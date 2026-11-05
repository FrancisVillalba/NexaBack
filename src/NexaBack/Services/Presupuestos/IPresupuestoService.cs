using NexaBack.Infrastructure.Data.Dtos.Presupuestos;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
namespace NexaBack.Services.Presupuestos;

public interface IPresupuestoService
{
    Task<PresupuestosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct);
    Task<PresupuestoDto> ObtenerAsync(int id, CancellationToken ct);
    Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct);
    Task<PresupuestoDto> GuardarAsync(int? id, PresupuestoGuardarRequest request, CancellationToken ct);
    Task<PresupuestoDto> AprobarAsync(int id, Guid version, CancellationToken ct);
    Task<PresupuestoDto> CancelarAsync(int id, PresupuestoCancelarRequest request, CancellationToken ct);
    Task<PresupuestoConversionDto> PrepararConversionAsync(int id, CancellationToken ct);
    Task<VentaDto> ConvertirAsync(int id, PresupuestoConvertirRequest request, CancellationToken ct);
}
