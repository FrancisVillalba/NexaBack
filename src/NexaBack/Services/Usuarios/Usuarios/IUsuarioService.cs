using NexaBack.Infrastructure.Data.Dtos.Usuarios;

namespace NexaBack.Services.Usuarios.Usuarios;

public interface IUsuarioService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioSesionDto> ObtenerSesionAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default);
    Task<UsuarioDto> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default);
}
