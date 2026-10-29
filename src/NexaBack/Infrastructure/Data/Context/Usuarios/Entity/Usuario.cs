using System.Text.Json.Serialization;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Usuario
{
    public int Id { get; set; }
    public required string NombreUsuario { get; set; }
    // Guardar únicamente el resultado de PasswordHasher<Usuario>, nunca texto plano.
    [JsonIgnore]
    public required string PasswordHash { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}
