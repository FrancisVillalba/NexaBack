using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Comercial.Entity;

// Datos propios del registro comercial: puede ser una persona física o una empresa.
public sealed class Cliente : IEntidadAuditable
{
    public int Id { get; set; }
    public required string NombreRazonSocial { get; set; }
    // Null identifica registros históricos que todavía no fueron clasificados.
    public int? TipoDocumentoId { get; set; }
    public TipoDocumento? TipoDocumento { get; set; }
    public required string Documento { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreadoEn { get; set; }
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}
