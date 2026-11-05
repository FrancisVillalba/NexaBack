using NexaBack.Infrastructure.Persistence;
namespace NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;

// Catálogo compartido por los productos, con auditoría centralizada.
public sealed class UnidadMedida : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Codigo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
