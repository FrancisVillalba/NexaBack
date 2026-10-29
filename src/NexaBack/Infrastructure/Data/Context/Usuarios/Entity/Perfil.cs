
namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Perfil
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
