using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Acceso : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Modulo { get; set; }
    public required string Accion { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
}

// Una fila vincula un permiso concreto con un perfil. La clave compuesta evita duplicados.
public sealed class PerfilAcceso : IEntidadAuditable
{
    public int PerfilId { get; set; }
    public int AccesoId { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public Acceso Acceso { get; set; } = null!;
}
