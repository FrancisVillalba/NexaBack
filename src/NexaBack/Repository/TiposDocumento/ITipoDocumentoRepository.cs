using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
namespace NexaBack.Repository.TiposDocumento;

public interface ITipoDocumentoRepository
{
    Task<IReadOnlyList<TipoDocumento>> ListarAsync(CancellationToken ct);
    Task<TipoDocumento?> ObtenerAsync(int id, CancellationToken ct);
}
