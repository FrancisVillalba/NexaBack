namespace NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

public sealed class Acceso
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Modulo { get; set; }
    public required string Accion { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
}

// Una fila vincula un permiso concreto con un perfil. La clave compuesta evita duplicados.
public sealed class PerfilAcceso
{
    public int PerfilId { get; set; }
    public int AccesoId { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public Acceso Acceso { get; set; } = null!;
}
