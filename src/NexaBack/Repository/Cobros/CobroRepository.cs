using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Cobros.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.Cobros;
using NexaBack.Services.Cobros;
using NexaBack.Services.Ventas;
using Npgsql;
namespace NexaBack.Repository.Cobros;

public sealed class CobroRepository(UsuariosDbContext context) : ICobroRepository
{
    public async Task<VentaCobrosDto> ListarAsync(int ventaId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        // La cabecera, los totales y el historial pertenecen a la misma instantánea.
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var venta = await context.Ventas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ventaId, ct) ?? throw new BusinessException("Venta no encontrada.", 404);
        var query = context.Cobros.AsNoTracking().Where(x => x.VentaId == ventaId);
        var cobrado = await query.Where(x => x.Estado == "Vigente").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).Select(CobroProyeccion.Dto).ToListAsync(ct);
        var resultado = new VentaCobrosDto(venta.Id, "VTA-" + venta.Id.ToString("D6"), venta.Estado, venta.Version, venta.Total,
            cobrado, CobroReglas.Saldo(venta.Estado, venta.Total, cobrado), CobroReglas.Estado(venta.Estado, venta.Total, cobrado), items, total, pagina, tamanoPagina);
        await transaccion.CommitAsync(ct);
        return resultado;
    }
    public async Task<CobroDto> RegistrarAsync(int ventaId, RegistrarCobroRequest request, string hash, CancellationToken ct)
    {
        try { return await RegistrarTransaccionAsync(ventaId, request, hash, ct); }
        catch (BusinessException ex) when (ex.StatusCode == 409)
        {
            // Recuperar un cobro que otro reintento terminó de confirmar con el mismo UUID.
            context.ChangeTracker.Clear();
            var existente = await context.Cobros.AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (existente is null) throw;
            ValidarReintento(existente, ventaId, hash);
            return await DtoAsync(existente.Id, ct);
        }
    }
    private Task<CobroDto> RegistrarTransaccionAsync(int ventaId, RegistrarCobroRequest request, string hash, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var anterior = await context.Cobros.AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (anterior is not null) { ValidarReintento(anterior, ventaId, hash); return await DtoAsync(anterior.Id, ct); }
            var venta = await context.Ventas.SingleOrDefaultAsync(x => x.Id == ventaId, ct) ?? throw new BusinessException("Venta no encontrada.", 404);
            VentaReglas.Version(venta.Version, request.VersionVenta);
            var cobrado = await context.Cobros.Where(x => x.VentaId == ventaId && x.Estado == "Vigente").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
            CobroReglas.Aplicar(venta.Estado, venta.Total, cobrado, request.Importe);
            var cobro = new Cobro { OperacionId = request.OperacionId, SolicitudHash = hash, VentaId = venta.Id, Importe = request.Importe,
                FechaCobro = request.FechaCobro, MedioPago = request.MedioPago, Referencia = request.Referencia, Observaciones = request.Observaciones };
            // Compartir la versión con la anulación de venta impide cobrar y devolver stock simultáneamente.
            venta.Version = Guid.NewGuid();
            context.Cobros.Add(cobro);
            await context.SaveChangesAsync(ct);
            return await DtoAsync(cobro.Id, ct);
        }, ct);

    public Task<CobroDto> AnularAsync(int ventaId, int cobroId, AnularCobroRequest request, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var cobro = await context.Cobros.SingleOrDefaultAsync(x => x.Id == cobroId && x.VentaId == ventaId, ct)
                ?? throw new BusinessException("Cobro no encontrado en esta venta.", 404);
            if (cobro.Estado == "Anulado")
            {
                if (cobro.MotivoAnulacion != request.Motivo) throw new BusinessException("El cobro ya se anuló con otro motivo. Actualizá el historial.", 409);
                return await DtoAsync(cobro.Id, ct);
            }
            if (cobro.Version != request.Version) throw new BusinessException("El cobro cambió. Actualizá el historial.", 409);
            var venta = await context.Ventas.SingleAsync(x => x.Id == ventaId, ct);
            cobro.Estado = "Anulado"; cobro.AnuladoEn = DateTime.UtcNow; cobro.MotivoAnulacion = request.Motivo; cobro.Version = Guid.NewGuid();
            venta.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return await DtoAsync(cobro.Id, ct);
        }, ct);

    private void ValidarReintento(Cobro cobro, int ventaId, string hash)
    {
        if (cobro.VentaId != ventaId || cobro.SolicitudHash != hash || cobro.CreadoPor != Usuario())
            throw new BusinessException("El identificador de operación ya se utilizó con otros datos.", 409);
    }
    private Task<CobroDto> DtoAsync(int id, CancellationToken ct) => context.Cobros.AsNoTracking().Where(x => x.Id == id).Select(CobroProyeccion.Dto).SingleAsync(ct);
    private int Usuario() => context.UsuarioAuditoriaId ?? throw new BusinessException("Se requiere una sesión válida.", 401);
    private async Task<CobroDto> TransaccionAsync(Func<Task<CobroDto>> accion, CancellationToken ct)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try { var resultado = await accion(); await transaccion.CommitAsync(ct); return resultado; }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Otra operación modificó el saldo. Actualizá el detalle y reintentá; conservá el identificador del cobro para evitar duplicados.", 409);
        }
    }
}
