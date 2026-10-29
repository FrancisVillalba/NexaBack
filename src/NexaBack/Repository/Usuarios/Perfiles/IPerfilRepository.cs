using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Repository.Usuarios.Perfiles;

public interface IPerfilRepository
{
    Task<IReadOnlyList<Perfil>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default);
    Task<Perfil?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string value, CancellationToken cancellationToken = default);
    Task AgregarAsync(Perfil entity, CancellationToken cancellationToken = default);
}

