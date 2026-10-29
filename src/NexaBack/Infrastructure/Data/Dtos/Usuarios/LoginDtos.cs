using System.ComponentModel.DataAnnotations;

namespace NexaBack.Infrastructure.Data.Dtos.Usuarios;

public sealed record LoginRequest(
    [Required, StringLength(100)] string NombreUsuario,
    [Required, StringLength(128)] string Password);

public sealed record UsuarioSesionDto(
    int UsuarioId, string NombreUsuario, string Nombre, string Apellido,
    string? Documento, int PerfilId, string Perfil);

public sealed record LoginResponse(
    string Token, string TipoToken, DateTimeOffset ExpiraEn, UsuarioSesionDto Usuario);
