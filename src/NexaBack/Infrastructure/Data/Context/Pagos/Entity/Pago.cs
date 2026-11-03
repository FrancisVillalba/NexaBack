using NexaBack.Infrastructure.Persistence;
using NexaBack.Infrastructure.Data.Context.Compras.Entity;
namespace NexaBack.Infrastructure.Data.Context.Pagos.Entity;

// Cada pago se aplica a una compra. La anulación conserva el registro y vuelve a abrir su saldo.
public sealed class Pago : IEntidadAuditable
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public required string SolicitudHash { get; set; }
    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
    public decimal Importe { get; set; }
    public DateOnly FechaPago { get; set; }
    public required string MedioPago { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "Vigente";
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime? AnuladoEn { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
