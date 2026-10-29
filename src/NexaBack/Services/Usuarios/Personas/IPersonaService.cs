using NexaBack.Infrastructure.Data.Dtos.Personas;

namespace NexaBack.Services.Usuarios.Personas;

public interface IPersonaService
{
    Task<IReadOnlyList<PersonaDto>> ListarAsync(int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default);
    Task<PersonaDto> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<PersonaDto> CrearAsync(CrearPersonaRequest request, CancellationToken cancellationToken = default);
}
