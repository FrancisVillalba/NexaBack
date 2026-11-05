namespace NexaBack.Infrastructure.Data.Dtos.Reportes;

public sealed record ReporteConsulta(string Tipo, DateOnly Desde, DateOnly Hasta, int? DepositoId, int Pagina = 1, int TamanoPagina = 20);
public sealed record ReporteDepositoDto(int Id, string Nombre, bool Activo);
// Importe es el total de la operación, deuda original o valorización, según el tipo explícito del reporte.
public sealed class ReporteFilaDto
{
    public int Id { get; init; }
    public string Referencia { get; init; } = "";
    public DateTime? Fecha { get; init; }
    public string Nombre { get; init; } = "";
    public string Estado { get; init; } = "";
    public decimal Importe { get; init; }
    public decimal Aplicado { get; init; }
    public decimal Saldo { get; init; }
    public decimal? Cantidad { get; init; }
    public string? Unidad { get; init; }
    public decimal? Costo { get; init; }
    public decimal? Minimo { get; init; }
}
public sealed record ReporteResumenDto(int Registros, decimal Importe, decimal Aplicado, decimal Saldo);
public sealed record ReporteDto(string Tipo, DateOnly Desde, DateOnly Hasta, int? DepositoId, string DepositoNombre, DateTime GeneradoEn,
    bool UsaPeriodo, ReporteResumenDto Resumen, IReadOnlyList<ReporteFilaDto> Items, int Pagina, int TamanoPagina,
    ReporteVentasDetalleDto? Ventas, IReadOnlyList<ReporteCategoriaDto> Categorias);
public sealed record InicioIndicadorDto(string Codigo, string Nombre, decimal Valor, int Cantidad, string Nota, string Ruta);
public sealed record InicioDto(DateOnly Desde, DateOnly Hasta, int? DepositoId, string DepositoNombre, DateTime GeneradoEn,
    IReadOnlyList<InicioIndicadorDto> Indicadores, IReadOnlyList<ReporteFilaDto> UltimasVentas, IReadOnlyList<ReporteFilaDto> StockBajo,
    IReadOnlyList<ReporteDiaDto> VentasSemana, IReadOnlyList<ReporteCategoriaDto> Categorias);

public sealed record ReporteDiaDto(DateOnly Fecha, decimal Total);
public sealed record ReporteProductoDto(int ProductoId, string Codigo, string Nombre, string Unidad, decimal Cantidad, decimal Bruto);
public sealed record ReporteVentasDetalleDto(decimal Costo, decimal MargenEstimado, decimal TicketPromedio, IReadOnlyList<ReporteDiaDto> Dias, IReadOnlyList<ReporteProductoDto> Productos);
public sealed record ReporteCategoriaDto(string Categoria, int Productos, decimal Valor);
