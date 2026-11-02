namespace NexaBack.Infrastructure.Data.Dtos.Productos;

// Los campos de auditoría se devuelven para consulta, pero no se reciben del formulario.
public sealed record ProductoDto(int Id, string Codigo, string Nombre, string? Descripcion,
    string? CodigoBarras, string? Marca, string Categoria, string UnidadMedida,
    decimal Costo, decimal PrecioVenta, decimal StockMinimo, bool Activo, Guid Version,
    DateTime CreadoEn, int? CreadoPor, DateTime? ModificadoEn, int? ModificadoPor);

public sealed record ProductosPaginaDto(IReadOnlyList<ProductoDto> Items, int Total, int Pagina, int TamanoPagina);

public sealed record GuardarProductoRequest(string Codigo, string Nombre, string? Descripcion,
    string? CodigoBarras, string? Marca, string Categoria, string UnidadMedida,
    decimal Costo, decimal PrecioVenta, decimal StockMinimo, Guid? Version = null);

public sealed record CambiarEstadoProductoRequest
{
    public required bool Activo { get; init; }
    public required Guid Version { get; init; }
}
