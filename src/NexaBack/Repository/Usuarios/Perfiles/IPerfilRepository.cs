using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Repository.Usuarios.Perfiles;

public interface IPerfilRepository
{
    Task<IReadOnlyList<Perfil>> ListarAsync(int offset, int limit, string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default);
    Task<Perfil?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
    Task<Perfil?> ObtenerParaActualizarAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string value, CancellationToken cancellationToken = default, int? excluirId = null);
    Task AgregarAsync(Perfil entity, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

