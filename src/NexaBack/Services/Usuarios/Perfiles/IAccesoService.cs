using NexaBack.Infrastructure.Data.Dtos.Perfiles;

namespace NexaBack.Services.Usuarios.Perfiles;

// Contrato de la lógica de accesos utilizado por los consumidores del servicio.
public interface IAccesoService
{
    Task<AccesoDto[]> ListarAsync(CancellationToken ct);
    Task<string[]> EfectivosAsync(int perfilId, string perfil, CancellationToken ct);
    Task<PerfilAccesosDto> ObtenerAsync(int id, CancellationToken ct);
    Task<PerfilAccesosDto> GuardarAsync(int id, GuardarAccesosRequest request, CancellationToken ct);
}
