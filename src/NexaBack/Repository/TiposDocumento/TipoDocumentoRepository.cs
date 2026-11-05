using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
namespace NexaBack.Repository.TiposDocumento;

public sealed class TipoDocumentoRepository(UsuariosDbContext context) : ITipoDocumentoRepository
{
    public async Task<IReadOnlyList<TipoDocumento>> ListarAsync(CancellationToken ct)
        => await context.TiposDocumento.AsNoTracking().OrderBy(x => x.Nombre).ThenBy(x => x.Id).ToListAsync(ct);
    // Seguimiento compartido para asociar una entidad existente al guardar el contacto.
    public Task<TipoDocumento?> ObtenerAsync(int id, CancellationToken ct) => context.TiposDocumento.SingleOrDefaultAsync(x => x.Id == id, ct);
}
