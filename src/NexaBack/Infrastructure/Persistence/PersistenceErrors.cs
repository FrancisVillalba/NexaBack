using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using Npgsql;

namespace NexaBack.Infrastructure.Persistence;

internal static class PersistenceErrors
{
    // Las restricciones de PostgreSQL son la última defensa ante solicitudes simultáneas.
    public static async Task SaveAsync(UsuariosDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Ya existe un registro con esos datos únicos.", 409);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("La persona o el perfil asociado ya no existe.", 409);
        }
    }
}
