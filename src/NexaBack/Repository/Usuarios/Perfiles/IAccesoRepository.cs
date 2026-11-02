using NexaBack.Infrastructure.Data.Dtos.Perfiles;

namespace NexaBack.Repository.Usuarios.Perfiles;

// Contrato de consulta y persistencia de permisos en la base de datos.
public interface IAccesoRepository
{
    Task<AccesoDto[]> ListarAsync(CancellationToken ct);
    Task<string[]> EfectivosAsync(int perfilId, bool administrador, CancellationToken ct);
    Task<PerfilAccesosDto> ObtenerAsync(int id, CancellationToken ct);
    Task<PerfilAccesosDto> GuardarAsync(int id, string[] codigos, string version, CancellationToken ct);
}
