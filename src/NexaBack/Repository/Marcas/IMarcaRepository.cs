using NexaBack.Infrastructure.Data.Context.Marcas.Entity;

namespace NexaBack.Repository.Marcas;

public interface IMarcaRepository
{
    Task<(IReadOnlyList<Marca> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Marca?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);
    Task AgregarAsync(Marca marca, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
    Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct);
}
