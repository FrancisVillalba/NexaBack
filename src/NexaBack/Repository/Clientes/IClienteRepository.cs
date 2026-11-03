using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
namespace NexaBack.Repository.Clientes;

public interface IClienteRepository
{
    Task<(IReadOnlyList<Cliente> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Cliente?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteDocumentoAsync(string tipoDocumento, string documento, CancellationToken ct, int? excluirId = null);
    Task AgregarAsync(Cliente contacto, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
}
