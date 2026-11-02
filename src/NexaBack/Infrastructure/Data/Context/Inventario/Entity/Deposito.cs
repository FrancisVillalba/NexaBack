using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Inventario.Entity;

public sealed class Deposito : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
