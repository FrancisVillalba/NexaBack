using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
using NexaBack.Infrastructure.Data.Dtos.TiposDocumento;
namespace NexaBack.Services.TiposDocumento;

public interface ITipoDocumentoService
{
    Task<IReadOnlyList<TipoDocumentoDto>> ListarAsync(CancellationToken ct);
    Task<TipoDocumento> ValidarAsync(int id, int? actualId, CancellationToken ct);
}
