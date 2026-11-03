namespace NexaBack.Infrastructure.Data.Dtos.Cobros;
public sealed record RegistrarCobroRequest(Guid OperacionId, Guid VersionVenta, decimal Importe, DateOnly FechaCobro, string MedioPago, string? Referencia, string? Observaciones);
public sealed record AnularCobroRequest(Guid Version, string Motivo);
public sealed record CobroDto(int Id, string Numero, int VentaId, string VentaNumero, decimal Importe, DateOnly FechaCobro,
    string MedioPago, string? Referencia, string? Observaciones, string Estado, Guid Version, DateTime? AnuladoEn, string? MotivoAnulacion,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);
public sealed record VentaCobrosDto(int VentaId, string VentaNumero, string VentaEstado, Guid VersionVenta, decimal TotalVenta,
    decimal Cobrado, decimal Saldo, string EstadoCobro, IReadOnlyList<CobroDto> Items, int TotalRegistros, int Pagina, int TamanoPagina);
