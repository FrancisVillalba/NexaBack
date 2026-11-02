using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Repository.Usuarios.Usuarios;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerConRelacionesAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorNombreConRelacionesAsync(string nombre, CancellationToken cancellationToken = default);
    Task<bool> ActualizarPasswordHashAsync(int id, string hashAnterior, string hashNuevo, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreUsuarioAsync(string value, CancellationToken cancellationToken = default, int? excluirId = null);
    Task<bool> ExistePersonaAsync(int personaId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Usuario entity, CancellationToken cancellationToken = default);
    Task<Usuario> ActualizarAsync(int id, string nombre, int perfilId, CancellationToken cancellationToken = default);
    Task CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default);
}

