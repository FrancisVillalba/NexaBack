namespace NexaBack.Infrastructure.Data.Dtos.Pagos;
public sealed record RegistrarPagoRequest(Guid OperacionId, Guid VersionCompra, decimal Importe, DateOnly FechaPago, string MedioPago, string? Referencia, string? Observaciones);
public sealed record AnularPagoRequest(Guid Version, string Motivo);
public sealed record PagoDto(int Id, string Numero, int CompraId, string CompraNumero, decimal Importe, DateOnly FechaPago,
    string MedioPago, string? Referencia, string? Observaciones, string Estado, Guid Version, DateTime? AnuladoEn, string? MotivoAnulacion,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record CompraPagosDto(int CompraId, string CompraNumero, string CompraEstado, Guid VersionCompra, decimal TotalCompra,
    decimal Pagado, decimal Saldo, string EstadoPago, IReadOnlyList<PagoDto> Items, int TotalRegistros, int Pagina, int TamanoPagina);
