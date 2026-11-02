using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Persistence;
using Npgsql;

namespace NexaBack.Repository.Inventario;

public sealed class DepositoRepository(UsuariosDbContext context) : IDepositoRepository
{
    public async Task<(IReadOnlyList<Deposito> Items, int Total)> ListarAsync(int offset, int limite, string? buscar, bool? activo, CancellationToken ct)
    {
        var query = context.Depositos.AsNoTracking();
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = "%" + buscar.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, patron, "\\") || EF.Functions.ILike(x.Descripcion ?? "", patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        return (await query.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(offset).Take(limite).ToListAsync(ct), total);
    }

    public Task<Deposito?> ObtenerAsync(int id, bool paraActualizar, CancellationToken ct)
        => (paraActualizar ? context.Depositos : context.Depositos.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
        => context.Depositos.AnyAsync(x => x.Id != excluirId && x.Nombre.Trim().ToLower() == nombre.ToLower(), ct);

    public async Task AgregarAsync(Deposito deposito, CancellationToken ct)
    {
        context.Depositos.Add(deposito);
        await GuardarCambiosAsync(ct);
    }

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try { await PersistenceErrors.SaveAsync(context, ct); }
        catch (DbUpdateConcurrencyException) { throw Conflicto(); }
    }

    public async Task CambiarEstadoAsync(int id, bool activo, Guid version, CancellationToken ct)
    {
        // Comprobar el saldo y cambiar el estado es una única operación, no dos consultas independientes.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var deposito = await context.Depositos.SingleOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new BusinessException("Depósito no encontrado.", 404);
            if (deposito.Version != version) throw Conflicto();
            if (deposito.Activo == activo) return;
            if (!activo && await context.Existencias.AnyAsync(x => x.DepositoId == id && x.Cantidad > 0, ct))
                throw new BusinessException("No podés desactivar un depósito con existencias. Transferí o ajustá sus saldos primero.", 409);
            deposito.Activo = activo;
            deposito.Version = Guid.NewGuid();
            await GuardarCambiosAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } })
        { throw Conflicto(); }
    }

    private static BusinessException Conflicto() => new("El depósito o sus existencias cambiaron. Actualizá el listado y volvé a intentar.", 409);
}
