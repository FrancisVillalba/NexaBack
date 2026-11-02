using NexaBack.Infrastructure.Data.Context.Inventario.Entity;

namespace NexaBack.Repository.Inventario;

public interface IDepositoRepository
{
    Task<(IReadOnlyList<Deposito> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Deposito?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);
    Task AgregarAsync(Deposito deposito, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
    Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct);
}
