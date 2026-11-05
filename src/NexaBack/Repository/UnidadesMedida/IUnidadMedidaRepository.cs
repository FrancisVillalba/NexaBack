using NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;
namespace NexaBack.Repository.UnidadesMedida;
public interface IUnidadMedidaRepository
{
    Task<(IReadOnlyList<UnidadMedida> Items, int Total)> ListarAsync(int offset, int limite, CancellationToken ct);
    Task<UnidadMedida?> ObtenerAsync(int id, CancellationToken ct);
}
