using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Inventario.Entity;

// Registro permanente: los errores se corrigen con otro movimiento, nunca borrando el historial.
public sealed class MovimientoInventario : IEntidadAuditable
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int DepositoId { get; set; }
    public Deposito Deposito { get; set; } = null!;
    public int? DepositoDestinoId { get; set; }
    public Deposito? DepositoDestino { get; set; }
    public required string Tipo { get; set; }
    // En ajustes, Cantidad es el saldo final solicitado. Variacion es el cambio efectivamente aplicado.
    public decimal Cantidad { get; set; }
    public decimal Variacion { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockPosterior { get; set; }
    public decimal? StockDestinoAnterior { get; set; }
    public decimal? StockDestinoPosterior { get; set; }
    public required string Motivo { get; set; }
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
