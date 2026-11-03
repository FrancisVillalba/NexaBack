using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Pagos.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.Pagos;
using NexaBack.Services.Pagos;
using NexaBack.Services.Compras;
using Npgsql;
namespace NexaBack.Repository.Pagos;

public sealed class PagoRepository(UsuariosDbContext context) : IPagoRepository
{
    public async Task<CompraPagosDto> ListarAsync(int compraId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        // La cabecera, los totales y el historial pertenecen a la misma instantánea.
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var compra = await context.Compras.AsNoTracking().SingleOrDefaultAsync(x => x.Id == compraId, ct) ?? throw new BusinessException("Compra no encontrada.", 404);
        var query = context.Pagos.AsNoTracking().Where(x => x.CompraId == compraId);
        var pagado = await query.Where(x => x.Estado == "Vigente").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).Select(PagoProyeccion.Dto).ToListAsync(ct);
        var resultado = new CompraPagosDto(compra.Id, "OC-" + compra.Id.ToString("D6"), compra.Estado, compra.Version, compra.Total,
            pagado, PagoReglas.Saldo(compra.Estado, compra.Total, pagado), PagoReglas.Estado(compra.Estado, compra.Total, pagado), items, total, pagina, tamanoPagina);
        await transaccion.CommitAsync(ct);
        return resultado;
    }
    public async Task<PagoDto> RegistrarAsync(int compraId, RegistrarPagoRequest request, string hash, CancellationToken ct)
    {
        try { return await RegistrarTransaccionAsync(compraId, request, hash, ct); }
        catch (BusinessException ex) when (ex.StatusCode == 409)
        {
            // Recuperar un pago que otro reintento terminó de confirmar con el mismo UUID.
            context.ChangeTracker.Clear();
            var existente = await context.Pagos.AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (existente is null) throw;
            ValidarReintento(existente, compraId, hash);
            return await DtoAsync(existente.Id, ct);
        }
    }
    private Task<PagoDto> RegistrarTransaccionAsync(int compraId, RegistrarPagoRequest request, string hash, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var anterior = await context.Pagos.AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (anterior is not null) { ValidarReintento(anterior, compraId, hash); return await DtoAsync(anterior.Id, ct); }
            var compra = await context.Compras.SingleOrDefaultAsync(x => x.Id == compraId, ct) ?? throw new BusinessException("Compra no encontrada.", 404);
            CompraReglas.ValidarVersion(compra.Version, request.VersionCompra);
            var pagado = await context.Pagos.Where(x => x.CompraId == compraId && x.Estado == "Vigente").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
            PagoReglas.Aplicar(compra.Estado, compra.Total, pagado, request.Importe);
            var pago = new Pago { OperacionId = request.OperacionId, SolicitudHash = hash, CompraId = compra.Id, Importe = request.Importe,
                FechaPago = request.FechaPago, MedioPago = request.MedioPago, Referencia = request.Referencia, Observaciones = request.Observaciones };
            // Cambiar la versión de la compra evita que dos pagos simultáneos excedan su saldo.
            compra.Version = Guid.NewGuid();
            context.Pagos.Add(pago);
            await context.SaveChangesAsync(ct);
            return await DtoAsync(pago.Id, ct);
        }, ct);

    public Task<PagoDto> AnularAsync(int compraId, int pagoId, AnularPagoRequest request, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var pago = await context.Pagos.SingleOrDefaultAsync(x => x.Id == pagoId && x.CompraId == compraId, ct)
                ?? throw new BusinessException("Pago no encontrado en esta compra.", 404);
            if (pago.Estado == "Anulado")
            {
                if (pago.MotivoAnulacion != request.Motivo) throw new BusinessException("El pago ya se anuló con otro motivo. Actualizá el historial.", 409);
                return await DtoAsync(pago.Id, ct);
            }
            if (pago.Version != request.Version) throw new BusinessException("El pago cambió. Actualizá el historial.", 409);
            var compra = await context.Compras.SingleAsync(x => x.Id == compraId, ct);
            pago.Estado = "Anulado"; pago.AnuladoEn = DateTime.UtcNow; pago.MotivoAnulacion = request.Motivo; pago.Version = Guid.NewGuid();
            compra.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return await DtoAsync(pago.Id, ct);
        }, ct);

    private void ValidarReintento(Pago pago, int compraId, string hash)
    {
        if (pago.CompraId != compraId || pago.SolicitudHash != hash || pago.CreadoPor != Usuario())
            throw new BusinessException("El identificador de operación ya se utilizó con otros datos.", 409);
    }
    private Task<PagoDto> DtoAsync(int id, CancellationToken ct) => context.Pagos.AsNoTracking().Where(x => x.Id == id).Select(PagoProyeccion.Dto).SingleAsync(ct);
    private int Usuario() => context.UsuarioAuditoriaId ?? throw new BusinessException("Se requiere una sesión válida.", 401);
    private async Task<PagoDto> TransaccionAsync(Func<Task<PagoDto>> accion, CancellationToken ct)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try { var resultado = await accion(); await transaccion.CommitAsync(ct); return resultado; }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Otra operación modificó el saldo. Actualizá el detalle y reintentá; conservá el identificador del pago para evitar duplicados.", 409);
        }
    }
}
