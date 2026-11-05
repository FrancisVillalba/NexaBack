using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Persona : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    // Los datos anteriores quedan sin clasificar hasta su próxima edición.
    public int? TipoDocumentoId { get; set; }
    public TipoDocumento? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
    public Usuario? Usuario { get; set; }
}
