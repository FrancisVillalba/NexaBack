using NexaBack.Infrastructure.Data.Dtos.Auditoria;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
namespace NexaBack.Infrastructure.Data.Dtos.Presupuestos;

public sealed record PresupuestoGuardarRequest(Guid OperacionId, Guid? Version, int ClienteId, int DepositoId, DateOnly FechaVencimiento,
    decimal Descuento, string? Observaciones, IReadOnlyList<VentaConfirmarLinea> Items);
public sealed record PresupuestoVersionRequest(Guid Version);
public sealed record PresupuestoCancelarRequest(Guid Version, string Motivo);
public sealed record PresupuestoProductoVersion(int ProductoId, Guid VersionProducto);
public sealed record PresupuestoConvertirRequest(Guid OperacionId, Guid Version, IReadOnlyList<PresupuestoProductoVersion> Items);
public sealed record PresupuestoConversionDto(Guid Version, VentaCotizacionDto Cotizacion);
public sealed record PresupuestoResumenDto(int Id, string Numero, string ClienteNombre, string? ClienteTipoDocumento, string ClienteDocumento,
    string DepositoNombre, string Estado, bool Vencido, DateOnly FechaVencimiento, decimal Total, Guid Version, DateTime CreadoEn, int? VentaId);
public sealed record PresupuestoLineaDto(int Id, int ProductoId, string ProductoCodigo, string ProductoNombre, string UnidadMedida,
    decimal Cantidad, decimal PrecioUnitario, decimal Subtotal);
public sealed record PresupuestoDto(int Id, string Numero, int ClienteId, string ClienteNombre, string? ClienteTipoDocumento, string ClienteDocumento,
    int DepositoId, string DepositoNombre, string Estado, bool Vencido, DateOnly FechaVencimiento, string? Observaciones,
    decimal Subtotal, decimal Descuento, decimal Total, Guid Version, DateTime? AprobadoEn, int? AprobadoPor,
    DateTime? CanceladoEn, string? MotivoCancelacion, int? VentaId, DateTime CreadoEn, int? CreadoPor,
    DateTime? ModificadoEn, int? ModificadoPor, IReadOnlyList<PresupuestoLineaDto> Items) : IAuditoriaVisible
{
    public AuditoriaNombres? NombresAuditoria { get; set; }
}
public sealed record PresupuestosPaginaDto(IReadOnlyList<PresupuestoResumenDto> Items, int Total, int Pagina, int TamanoPagina);
