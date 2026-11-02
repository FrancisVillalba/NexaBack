using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;
using NexaBack.Repository.Usuarios.Perfiles;

namespace NexaBack.Services.Usuarios.Perfiles;

public sealed class AccesoService(IAccesoRepository repository) : IAccesoService
{
    public Task<AccesoDto[]> ListarAsync(CancellationToken ct) => repository.ListarAsync(ct);
    public Task<string[]> EfectivosAsync(int perfilId, string perfil, CancellationToken ct)
        => repository.EfectivosAsync(perfilId, perfil == "Administrador", ct);
    public Task<PerfilAccesosDto> ObtenerAsync(int id, CancellationToken ct) => repository.ObtenerAsync(id, ct);
    public Task<PerfilAccesosDto> GuardarAsync(int id, GuardarAccesosRequest request, CancellationToken ct)
    {
        if (request.Codigos is null || request.Codigos.Length > 200 || request.Codigos.Any(string.IsNullOrWhiteSpace)
            || string.IsNullOrWhiteSpace(request.Version)) throw new BusinessException("La selección de accesos no es válida.");
        return repository.GuardarAsync(id, request.Codigos.Distinct(StringComparer.Ordinal).ToArray(), request.Version, ct);
    }
}
