using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Persistence;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
namespace NexaBack.Infrastructure.Data.Context.Ventas.Entity;

// Datos históricos de la venta: los cambios posteriores del catálogo no alteran sus importes.
public sealed class Venta : IEntidadAuditable
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public required string SolicitudHash { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int DepositoId { get; set; }
    public Deposito Deposito { get; set; } = null!;
    public string Estado { get; set; } = "Confirmada";
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime? AnuladaEn { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<VentaDetalle> Detalles { get; set; } = [];
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}

public sealed class VentaDetalle : IEntidadAuditable
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public required string UnidadMedida { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public int MovimientoSalidaId { get; set; }
    public MovimientoInventario MovimientoSalida { get; set; } = null!;
    public int? MovimientoAnulacionId { get; set; }
    public MovimientoInventario? MovimientoAnulacion { get; set; }
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
