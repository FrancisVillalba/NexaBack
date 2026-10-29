using NexaBack.Infrastructure.Data.Dtos.Perfiles;

namespace NexaBack.Services.Usuarios.Perfiles;

public interface IPerfilService
{
    Task<IReadOnlyList<PerfilDto>> ListarAsync(int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default);
    Task<PerfilDto> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<PerfilDto> CrearAsync(CrearPerfilRequest request, CancellationToken cancellationToken = default);
}
