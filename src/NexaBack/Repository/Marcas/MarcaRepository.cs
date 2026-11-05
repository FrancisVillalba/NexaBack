using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Marcas.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Marcas;

public sealed class MarcaRepository(UsuariosDbContext context) : IMarcaRepository
{
    public async Task<(IReadOnlyList<Marca> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct)
    {
        var query = context.Marcas.AsNoTracking();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, patron, "\\") || EF.Functions.ILike(x.Descripcion ?? "", patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        return (await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), total);
    }

    public Task<Marca?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct)
        => (paraActualizar ? context.Marcas : context.Marcas.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
        => context.Marcas.AnyAsync(x => x.Id != excluirId && x.Nombre.Trim().ToLower() == nombre.ToLower(), ct);

    public async Task AgregarAsync(Marca marca, CancellationToken ct)
    {
        context.Marcas.Add(marca);
        await GuardarCambiosAsync(ct);
    }

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try { await PersistenceErrors.SaveAsync(context, ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }

    public async Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct)
    {
        var marca = await context.Marcas.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Marca no encontrada.", 404);
        if (marca.Version != version) throw Conflicto();
        if (marca.Activo == activo) return;
        marca.Activo = activo; marca.Version = Guid.NewGuid();
        await GuardarCambiosAsync(ct);
    }

    private static BusinessException Conflicto() => new("La marca cambió. Actualizá el listado y volvé a intentar.", 409);
}
