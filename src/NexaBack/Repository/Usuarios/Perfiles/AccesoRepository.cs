using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;
using NexaBack.Infrastructure.Security;

namespace NexaBack.Repository.Usuarios.Perfiles;

public sealed class AccesoRepository(UsuariosDbContext context) : IAccesoRepository
{
    public async Task<AccesoDto[]> ListarAsync(CancellationToken ct)
        => (await context.Accesos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Id).ToListAsync(ct))
            .Select(x => new AccesoDto(x.Codigo, x.Modulo, x.Accion, x.Nombre, AccessCatalog.IsAdministrative(x.Modulo))).ToArray();

    public async Task<string[]> EfectivosAsync(int perfilId, bool administrador, CancellationToken ct)
    {
        if (administrador) return AccessCatalog.Definitions.Select(x => x.Codigo).ToArray();
        // No se usan permisos viejos del JWT: se consulta la asignación vigente en PostgreSQL.
        return await context.PerfilesAccesos.AsNoTracking()
            .Where(x => x.PerfilId == perfilId && x.Acceso.Activo && x.Perfil.Activo)
            .Where(x => x.Acceso.Modulo != "personas" && x.Acceso.Modulo != "usuarios"
                && x.Acceso.Modulo != "perfiles" && x.Acceso.Modulo != "permisos" && x.Acceso.Modulo != "configuracion")
            .Select(x => x.Acceso.Codigo).OrderBy(x => x).ToArrayAsync(ct);
    }

    public async Task<PerfilAccesosDto> ObtenerAsync(int id, CancellationToken ct)
    {
        var perfil = await context.Perfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Perfil no encontrado.", 404);
        var admin = perfil.Nombre == "Administrador";
        var codigos = await EfectivosAsync(id, admin, ct);
        // La matriz permite preparar perfiles inactivos, aunque todavía no puedan iniciar sesión.
        if (!admin && !perfil.Activo)
            codigos = await context.PerfilesAccesos.AsNoTracking().Where(x => x.PerfilId == id && x.Acceso.Activo)
                .Select(x => x.Acceso.Codigo).OrderBy(x => x).ToArrayAsync(ct);
        return new(id, perfil.Nombre, admin, codigos, Version(codigos));
    }

    public async Task<PerfilAccesosDto> GuardarAsync(int id, string[] codigos, string version, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var actual = await ObtenerAsync(id, ct);
            if (actual.Administrador) throw new BusinessException("Administrador conserva todos los accesos y no se puede restringir.", 409);
            if (actual.Version != version) throw new BusinessException("Los permisos cambiaron mientras editabas. Actualizá antes de volver a guardar.", 409);
            var accesos = await context.Accesos.Where(x => x.Activo && codigos.Contains(x.Codigo)).ToListAsync(ct);
            if (accesos.Count != codigos.Length) throw new BusinessException("Se indicaron accesos inexistentes o inactivos.");
            if (accesos.Any(x => AccessCatalog.IsAdministrative(x.Modulo)))
                throw new BusinessException("La administración del sistema es exclusiva de Administrador.");
            if (accesos.Any(x => x.Accion != "ver" && !codigos.Contains(x.Modulo + ".ver")))
                throw new BusinessException("Para habilitar acciones de un módulo también debés habilitar Ver.");
            var previous = await context.PerfilesAccesos.Where(x => x.PerfilId == id).ToListAsync(ct);
            var selected = accesos.Select(x => x.Id).ToHashSet();
            context.PerfilesAccesos.RemoveRange(previous.Where(x => !selected.Contains(x.AccesoId)));
            var retained = previous.Select(x => x.AccesoId).ToHashSet();
            context.PerfilesAccesos.AddRange(accesos.Where(x => !retained.Contains(x.Id))
                .Select(x => new PerfilAcceso { PerfilId = id, AccesoId = x.Id }));
            await context.SaveChangesAsync(ct);
            var result = await ObtenerAsync(id, ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } })
        { throw new BusinessException("Otro administrador modificó los accesos. Actualizá y volvé a intentar.", 409); }
    }

    private static string Version(string[] codes) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", codes.Order()))));
}
