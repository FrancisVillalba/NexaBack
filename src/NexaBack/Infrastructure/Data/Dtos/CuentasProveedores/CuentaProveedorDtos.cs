using NexaBack.Infrastructure.Data.Dtos.Pagos;
namespace NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
public sealed record CuentaCompraDto(int Id, string Numero, DateTime Fecha, string Estado, decimal Total, decimal Pagado, decimal Saldo, string EstadoPago);
public sealed record CuentaProveedorDto(int ProveedorId, string ProveedorNombre, string? TipoDocumento, string Documento, bool Activo,
    decimal TotalCompras, decimal TotalPagos, decimal Saldo, IReadOnlyList<CuentaCompraDto> Compras, int TotalRegistrosCompras, int PaginaCompras,
    IReadOnlyList<PagoDto> Pagos, int TotalRegistrosPagos, int PaginaPagos, int TamanoPagina);
