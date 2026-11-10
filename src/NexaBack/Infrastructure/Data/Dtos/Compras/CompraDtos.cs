namespace NexaBack.Infrastructure.Data.Dtos.Compras;

public sealed record CompraLineaEntrada(int ProductoId, decimal Cantidad, decimal CostoUnitario);
public record CompraDatosRequest(int ProveedorId, int DepositoId, string? Observaciones, IReadOnlyList<CompraLineaEntrada> Items);
public sealed record CrearCompraRequest(Guid OperacionId, int ProveedorId, int DepositoId, string? Observaciones, IReadOnlyList<CompraLineaEntrada> Items)
    : CompraDatosRequest(ProveedorId, DepositoId, Observaciones, Items);
public sealed record EditarCompraRequest(Guid Version, int ProveedorId, int DepositoId, string? Observaciones, IReadOnlyList<CompraLineaEntrada> Items)
    : CompraDatosRequest(ProveedorId, DepositoId, Observaciones, Items);
public sealed record CompraEstadoRequest(Guid Version);
public sealed record CompraResumenDto(int Id, string Numero, string ProveedorNombre, string? ProveedorTipoDocumento, string? ProveedorDocumento, string DepositoNombre,
    string Estado, decimal Total, Guid Version, DateTime CreadoEn);
public sealed record CompraLineaDto(int Id, int ProductoId, string ProductoCodigo, string ProductoNombre, string UnidadMedida,
    decimal Cantidad, decimal CostoUnitario, decimal Subtotal, int? MovimientoId);
public sealed record CompraDto(int Id, string Numero, int ProveedorId, string ProveedorNombre, string? ProveedorTipoDocumento, string? ProveedorDocumento,
    int DepositoId, string DepositoNombre, string Estado, string? Observaciones, decimal Total, Guid Version, DateTime? RecibidoEn,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor, IReadOnlyList<CompraLineaDto> Items);
public sealed record ComprasPaginaDto(IReadOnlyList<CompraResumenDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record CompraProveedorDto(int Id, string Nombre, string? TipoDocumento, string? Documento);
public sealed record CompraProductoDto(int Id, string Codigo, string Nombre, string UnidadMedida, decimal Costo);
public sealed record CompraDepositoDto(int Id, string Nombre);
