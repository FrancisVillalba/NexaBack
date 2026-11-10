using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Persistence;
using NexaBack.Infrastructure.Data.Context.Ventas.Entity;
namespace NexaBack.Infrastructure.Data.Context.Presupuestos.Entity;

// Guarda la propuesta histórica. No posee movimientos ni reservas de inventario.
public sealed class Presupuesto : IEntidadAuditable
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public required string SolicitudHash { get; set; }
    public Guid? EdicionOperacionId { get; set; }
    public string? EdicionHash { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int DepositoId { get; set; }
    public Deposito Deposito { get; set; } = null!;
    public string Estado { get; set; } = "Borrador";
    public DateOnly FechaVencimiento { get; set; }
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime? AprobadoEn { get; set; }
    public int? AprobadoPor { get; set; }
    public DateTime? CanceladoEn { get; set; }
    public string? MotivoCancelacion { get; set; }
    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }
    public List<PresupuestoDetalle> Detalles { get; set; } = [];
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
public sealed class PresupuestoDetalle : IEntidadAuditable
{
    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    public Presupuesto Presupuesto { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public required string UnidadMedida { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
