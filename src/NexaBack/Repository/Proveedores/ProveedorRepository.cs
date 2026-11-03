using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Proveedores;

// Consulta solamente la tabla comercial de este módulo; no accede a Personas.
public sealed class ProveedorRepository(UsuariosDbContext context) : IProveedorRepository
{
    public async Task<(IReadOnlyList<Proveedor> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct)
    {
        var query = context.Proveedores.AsNoTracking();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.NombreRazonSocial, patron, "\\")
                || EF.Functions.ILike(x.Documento, patron, "\\") || EF.Functions.ILike(x.Correo ?? "", patron, "\\")
                || EF.Functions.ILike(x.Telefono ?? "", patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        return (await query.OrderBy(x => x.NombreRazonSocial).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), total);
    }
    public Task<Proveedor?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct)
        => (paraActualizar ? context.Proveedores : context.Proveedores.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExisteDocumentoAsync(string tipoDocumento, string documento, CancellationToken ct, int? excluirId = null)
        => context.Proveedores.AnyAsync(x => x.Documento == documento && (x.TipoDocumento == tipoDocumento || x.TipoDocumento == null) && (!excluirId.HasValue || x.Id != excluirId.Value), ct);
    public async Task AgregarAsync(Proveedor contacto, CancellationToken ct)
    {
        context.Proveedores.Add(contacto);
        await GuardarCambiosAsync(ct);
    }
    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try { await PersistenceErrors.SaveAsync(context, ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("El registro cambió mientras lo editabas. Actualizá el listado y volvé a intentar.", 409);
        }
    }
}
