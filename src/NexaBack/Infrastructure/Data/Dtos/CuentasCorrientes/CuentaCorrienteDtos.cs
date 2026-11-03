using NexaBack.Infrastructure.Data.Dtos.Cobros;
namespace NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
public sealed record CuentaVentaDto(int Id, string Numero, DateTime Fecha, string Estado, decimal Total, decimal Cobrado, decimal Saldo, string EstadoCobro);
public sealed record CuentaCorrienteDto(int ClienteId, string ClienteNombre, string? TipoDocumento, string Documento, bool Activo,
    decimal TotalVentas, decimal TotalCobros, decimal Saldo, IReadOnlyList<CuentaVentaDto> Ventas, int TotalRegistrosVentas, int PaginaVentas,
    IReadOnlyList<CobroDto> Cobros, int TotalRegistrosCobros, int PaginaCobros, int TamanoPagina);
