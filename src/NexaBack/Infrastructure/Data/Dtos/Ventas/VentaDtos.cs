using NexaBack.Infrastructure.Data.Dtos.Auditoria;
namespace NexaBack.Infrastructure.Data.Dtos.Ventas;
public sealed record VentaLineaSolicitud(int ProductoId, decimal Cantidad);
public sealed record VentaCotizarRequest(int ClienteId, int DepositoId, decimal Descuento, IReadOnlyList<VentaLineaSolicitud> Items);
public sealed record VentaConfirmarLinea(int ProductoId, decimal Cantidad, Guid VersionProducto);
public sealed record VentaConfirmarRequest(Guid OperacionId, int ClienteId, int DepositoId, decimal Descuento, string? Observaciones, IReadOnlyList<VentaConfirmarLinea> Items);
public sealed record VentaAnularRequest(Guid Version, string Motivo);
public sealed record VentaCotizacionLinea(int ProductoId, string Codigo, string Nombre, string UnidadMedida, decimal Cantidad,
    decimal PrecioUnitario, decimal Subtotal, decimal Disponible, Guid VersionProducto);
public sealed record VentaCotizacionDto(int ClienteId, string ClienteNombre, int DepositoId, string DepositoNombre,
    decimal Subtotal, decimal Descuento, decimal Total, IReadOnlyList<VentaCotizacionLinea> Items);
public sealed record VentaResumenDto(int Id, string Numero, string ClienteNombre, string? ClienteTipoDocumento,
    string ClienteDocumento, string DepositoNombre, string Estado, decimal Total, Guid Version, DateTime CreadoEn);
public sealed record VentaLineaDto(int Id, int ProductoId, string ProductoCodigo, string ProductoNombre, string UnidadMedida,
    decimal Cantidad, decimal PrecioUnitario, decimal Subtotal, int MovimientoSalidaId, int? MovimientoAnulacionId);
public sealed record VentaDto(int Id, string Numero, int ClienteId, string ClienteNombre, string? ClienteTipoDocumento,
    string ClienteDocumento, int DepositoId, string DepositoNombre, string Estado, string? Observaciones,
    decimal Subtotal, decimal Descuento, decimal Total, Guid Version, DateTime? AnuladaEn, string? MotivoAnulacion,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor, IReadOnlyList<VentaLineaDto> Items) : IAuditoriaVisible
{
    public AuditoriaNombres? NombresAuditoria { get; set; }
}
public sealed record VentasPaginaDto(IReadOnlyList<VentaResumenDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record VentaClienteDto(int Id, string Nombre, string? TipoDocumento, string Documento);
public sealed record VentaDepositoDto(int Id, string Nombre);
public sealed record VentaProductoDto(int Id, string Codigo, string Nombre, string UnidadMedida, decimal PrecioVenta, decimal Disponible);
