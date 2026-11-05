using NexaBack.Infrastructure.Persistence;
namespace NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;

// Catálogo compartido. Clientes, proveedores y personas guardan su FK, no una lista fija de textos.
public sealed class TipoDocumento : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
