using NexaBack.Infrastructure.Data.Dtos.Usuarios;

namespace NexaBack.Infrastructure.Security;

public interface ITokenService
{
    LoginResponse Crear(UsuarioSesionDto usuario);
}
