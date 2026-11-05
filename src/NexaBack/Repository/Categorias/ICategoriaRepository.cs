using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
namespace NexaBack.Repository.Categorias;
public interface ICategoriaRepository
{
    Task<(IReadOnlyList<Categoria> Items, int Total)> ListarAsync(int offset, int limite, CancellationToken ct);
    Task<Categoria?> ObtenerAsync(int id, CancellationToken ct);
}
