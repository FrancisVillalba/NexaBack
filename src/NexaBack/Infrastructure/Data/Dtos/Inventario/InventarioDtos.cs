namespace NexaBack.Infrastructure.Data.Dtos.Inventario;

public sealed record DepositoDto(int Id, string Nombre, bool Activo);
public sealed record ExistenciaDto(int ProductoId, string Codigo, string Nombre, string Categoria, string UnidadMedida,
    bool Activo, decimal StockMinimo, decimal Cantidad, Guid? Version);
public sealed record InventarioPaginaDto(IReadOnlyList<ExistenciaDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record MovimientoDto(int Id, Guid OperacionId, int ProductoId, string Codigo, string Nombre, string UnidadMedida,
    int DepositoId, string Deposito, int? DepositoDestinoId, string? DepositoDestino, string Tipo, decimal Cantidad,
    decimal Variacion, decimal StockAnterior, decimal StockPosterior, decimal? StockDestinoAnterior,
    decimal? StockDestinoPosterior, string Motivo, DateTime CreadoEn, int? CreadoPor);
public sealed record MovimientosPaginaDto(IReadOnlyList<MovimientoDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record RegistrarMovimientoRequest(Guid OperacionId, int ProductoId, int DepositoId, int? DepositoDestinoId,
    string Tipo, decimal Cantidad, string Motivo, Guid? VersionOrigen = null);
