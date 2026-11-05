using NexaBack.Infrastructure.Persistence;
namespace NexaBack.Infrastructure.Data.Context.Marcas.Entity;

// Catálogo independiente. Desactivar una marca conserva sus productos y su historial.
public sealed class Marca : IEntidadAuditable
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
