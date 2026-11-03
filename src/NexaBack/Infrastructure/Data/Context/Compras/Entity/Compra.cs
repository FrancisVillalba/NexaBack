using NexaBack.Infrastructure.Persistence;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;

namespace NexaBack.Infrastructure.Data.Context.Compras.Entity;

// La orden conserva una copia de los nombres y precios acordados, aunque luego cambie el catálogo.
public sealed class Compra : IEntidadAuditable
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public required string SolicitudHash { get; set; }
    public int ProveedorId { get; set; }
    public required string ProveedorNombre { get; set; }
    public string? ProveedorTipoDocumento { get; set; }
    public string? ProveedorDocumento { get; set; }
    public int DepositoId { get; set; }
    public required string DepositoNombre { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime? RecibidoEn { get; set; }
    public List<CompraDetalle> Detalles { get; set; } = [];
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}

public sealed class CompraDetalle : IEntidadAuditable
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
    public int ProductoId { get; set; }
    public required string ProductoCodigo { get; set; }
    public required string ProductoNombre { get; set; }
    public required string UnidadMedida { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    // Relación directa con el movimiento real que se generó al recibir esta línea.
    public int? MovimientoId { get; set; }
    public MovimientoInventario? Movimiento { get; set; }
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
