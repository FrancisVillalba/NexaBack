using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Categorias;

public sealed class CategoriaRepository(UsuariosDbContext context) : ICategoriaRepository
{
    public async Task<(IReadOnlyList<Categoria> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct)
    {
        var query = context.Categorias.AsNoTracking();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, patron, "\\") || EF.Functions.ILike(x.Descripcion ?? "", patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        return (await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), total);
    }

    public Task<Categoria?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct)
        => (paraActualizar ? context.Categorias : context.Categorias.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
        => context.Categorias.AnyAsync(x => x.Id != excluirId && x.Nombre.Trim().ToLower() == nombre.ToLower(), ct);

    public async Task AgregarAsync(Categoria categoria, CancellationToken ct)
    {
        context.Categorias.Add(categoria);
        await GuardarCambiosAsync(ct);
    }

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try { await PersistenceErrors.SaveAsync(context, ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }

    public async Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct)
    {
        var categoria = await context.Categorias.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Categoría no encontrada.", 404);
        if (categoria.Version != version) throw Conflicto();
        if (categoria.Activo == activo) return;
        categoria.Activo = activo; categoria.Version = Guid.NewGuid();
        await GuardarCambiosAsync(ct);
    }

    private static BusinessException Conflicto() => new("La categoría cambió. Actualizá el listado y volvé a intentar.", 409);
}
