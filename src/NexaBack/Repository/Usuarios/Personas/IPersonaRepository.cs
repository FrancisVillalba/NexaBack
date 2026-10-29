using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Repository.Usuarios.Personas;

public interface IPersonaRepository
{
    Task<IReadOnlyList<Persona>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default);
    Task<Persona?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteDocumentoAsync(string value, CancellationToken cancellationToken = default);
    Task AgregarAsync(Persona entity, CancellationToken cancellationToken = default);
}

