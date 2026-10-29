namespace NexaBack.Infrastructure.Data.Dtos.Usuarios;

public sealed record CrearUsuarioRequest(string NombreUsuario, string Password, int PersonaId, int PerfilId);
public sealed record UsuarioDto(int Id, string NombreUsuario, int PersonaId, int PerfilId, bool Activo, DateTime CreadoEn);
