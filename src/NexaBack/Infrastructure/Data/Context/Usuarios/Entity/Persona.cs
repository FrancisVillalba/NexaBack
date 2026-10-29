namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Persona
{
    public int Id { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public string? Documento { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public Usuario? Usuario { get; set; }
}
