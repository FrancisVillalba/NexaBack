using NexaBack.Infrastructure.Data.Context.Categorias.Entity;

namespace NexaBack.Repository.Categorias;

public interface ICategoriaRepository
{
    Task<(IReadOnlyList<Categoria> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Categoria?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);
    Task AgregarAsync(Categoria categoria, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
    Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct);
}
