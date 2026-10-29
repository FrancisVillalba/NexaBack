using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Repository.Usuarios.Personas;

public sealed class PersonaRepository(UsuariosDbContext context) : IPersonaRepository
{
    public async Task<IReadOnlyList<Persona>> ListarAsync(int offset, int limit, CancellationToken cancellationToken = default)
        => await context.Personas.AsNoTracking().OrderBy(x => x.Nombres).ThenBy(x => x.Id)
            .Skip(offset).Take(limit).ToListAsync(cancellationToken);

    public Task<Persona?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
        => context.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExisteDocumentoAsync(string value, CancellationToken cancellationToken = default)
        => context.Personas.AnyAsync(x => x.Documento == value, cancellationToken);

    public async Task AgregarAsync(Persona entity, CancellationToken cancellationToken = default)
    {
        context.Personas.Add(entity);
        await PersistenceErrors.SaveAsync(context, cancellationToken);
    }
}

