using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
namespace NexaBack.Repository.Proveedores;

public interface IProveedorRepository
{
    Task<(IReadOnlyList<Proveedor> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct);
    Task<Proveedor?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct);
    Task<bool> ExisteDocumentoAsync(string tipoDocumento, string documento, CancellationToken ct, int? excluirId = null);
    Task AgregarAsync(Proveedor contacto, CancellationToken ct);
    Task GuardarCambiosAsync(CancellationToken ct);
}
