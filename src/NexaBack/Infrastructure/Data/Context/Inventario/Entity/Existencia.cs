using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Inventario.Entity;

public sealed class Existencia : IEntidadAuditable
{
    public int ProductoId { get; set; }
    public int DepositoId { get; set; }
    public decimal Cantidad { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public Producto Producto { get; set; } = null!;
    public Deposito Deposito { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
