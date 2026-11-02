
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Perfil : IEntidadAuditable
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }
    public int? ModificadoPor { get; set; }
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
