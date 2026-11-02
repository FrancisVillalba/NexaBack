namespace NexaBack.Infrastructure.Data.Dtos.Usuarios;

public sealed record CrearUsuarioRequest(string NombreUsuario, string Password, int PersonaId, int PerfilId);
// La identidad personal se conserva. La contraseña se administra en otra operación.
public sealed record ActualizarUsuarioRequest(string NombreUsuario, int PerfilId);
public sealed record CambiarPasswordUsuarioRequest(string Password);
public sealed record CambiarEstadoUsuarioRequest
{
    public required bool Activo { get; init; }
}
public sealed record UsuarioDto(int Id, string NombreUsuario, int PersonaId, int PerfilId, bool Activo, DateTime CreadoEn);
