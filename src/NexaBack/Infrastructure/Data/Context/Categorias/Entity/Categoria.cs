using NexaBack.Infrastructure.Persistence;
namespace NexaBack.Infrastructure.Data.Context.Categorias.Entity;

// Catálogo compartido por los productos, con auditoría centralizada.
public sealed class Categoria : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
