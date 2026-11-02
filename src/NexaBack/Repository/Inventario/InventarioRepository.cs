using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.Inventario;
using NexaBack.Services.Inventario;
using Npgsql;

namespace NexaBack.Repository.Inventario;

public sealed class InventarioRepository(UsuariosDbContext context) : IInventarioRepository
{
    public async Task<IReadOnlyList<DepositoDto>> DepositosAsync(CancellationToken ct)
        => await context.Depositos.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new DepositoDto(x.Id, x.Nombre, x.Activo)).ToListAsync(ct);

    public async Task<InventarioPaginaDto> ListarAsync(int depositoId, int pagina, int tamanoPagina, string? buscar, string estado, bool? activo, CancellationToken ct)
    {
        if (!await context.Depositos.AnyAsync(x => x.Id == depositoId, ct)) throw new BusinessException("Depósito no encontrado.", 404);
        var productos = context.Productos.AsNoTracking();
        if (activo.HasValue) productos = productos.Where(x => x.Activo == activo.Value);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = Patron(buscar);
            productos = productos.Where(x => EF.Functions.ILike(x.Codigo, patron, "\\") || EF.Functions.ILike(x.Nombre, patron, "\\")
                || EF.Functions.ILike(x.Categoria, patron, "\\") || EF.Functions.ILike(x.CodigoBarras ?? "", patron, "\\"));
        }
        // Un producto nuevo también aparece: sin movimientos, su existencia es cero.
        var query = productos.Select(p => new
        {
            Producto = p,
            Cantidad = context.Existencias.Where(e => e.ProductoId == p.Id && e.DepositoId == depositoId).Select(e => (decimal?)e.Cantidad).FirstOrDefault() ?? 0m,
            Version = context.Existencias.Where(e => e.ProductoId == p.Id && e.DepositoId == depositoId).Select(e => (Guid?)e.Version).FirstOrDefault()
        });
        query = estado switch
        {
            "bajo" => query.Where(x => x.Cantidad <= x.Producto.StockMinimo),
            "sin_stock" => query.Where(x => x.Cantidad == 0),
            "disponible" => query.Where(x => x.Cantidad > x.Producto.StockMinimo),
            _ => query
        };
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Producto.Nombre).ThenBy(x => x.Producto.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(x => new ExistenciaDto(x.Producto.Id, x.Producto.Codigo, x.Producto.Nombre, x.Producto.Categoria,
                x.Producto.UnidadMedida, x.Producto.Activo, x.Producto.StockMinimo, x.Cantidad, x.Version)).ToListAsync(ct);
        return new(items, total, pagina, tamanoPagina);
    }

    public async Task<MovimientosPaginaDto> MovimientosAsync(int? depositoId, int? productoId, int pagina, int tamanoPagina, string? buscar, CancellationToken ct)
    {
        var query = context.MovimientosInventario.AsNoTracking();
        if (depositoId.HasValue) query = query.Where(x => x.DepositoId == depositoId || x.DepositoDestinoId == depositoId);
        if (productoId.HasValue) query = query.Where(x => x.ProductoId == productoId);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = Patron(buscar);
            query = query.Where(x => EF.Functions.ILike(x.Producto.Codigo, patron, "\\")
                || EF.Functions.ILike(x.Producto.Nombre, patron, "\\") || EF.Functions.ILike(x.Motivo, patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).Select(Proyeccion).ToListAsync(ct);
        return new(items, total, pagina, tamanoPagina);
    }

    public async Task<MovimientoDto> RegistrarAsync(RegistrarMovimientoRequest request, CancellationToken ct)
    {
        var usuarioId = context.UsuarioAuditoriaId ?? throw new BusinessException("La operación requiere un usuario autenticado.", 401);
        // Serializable protege tanto saldos existentes como la primera entrada de un producto en un depósito.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var anterior = await context.MovimientosInventario.AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (anterior is not null)
            {
                if (anterior.CreadoPor != usuarioId || anterior.ProductoId != request.ProductoId || anterior.DepositoId != request.DepositoId
                    || anterior.DepositoDestinoId != request.DepositoDestinoId || anterior.Tipo != request.Tipo
                    || anterior.Cantidad != request.Cantidad || anterior.Motivo != request.Motivo)
                    throw new BusinessException("Ese identificador de operación ya se utilizó con otros datos.", 409);
                return await context.MovimientosInventario.AsNoTracking().Where(x => x.Id == anterior.Id).Select(Proyeccion).SingleAsync(ct);
            }
            var producto = await context.Productos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ProductoId, ct)
                ?? throw new BusinessException("Producto no encontrado.", 404);
            if (!producto.Activo) throw new BusinessException("El producto está inactivo. Reactivalo antes de registrar movimientos.");
            var origen = await context.Depositos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.DepositoId, ct);
            if (origen is null || !origen.Activo) throw new BusinessException("El depósito de origen no existe o está inactivo.");
            if (request.DepositoDestinoId.HasValue && !await context.Depositos.AnyAsync(x => x.Id == request.DepositoDestinoId && x.Activo, ct))
                throw new BusinessException("El depósito de destino no existe o está inactivo.");
            var saldo = await ObtenerSaldoAsync(request.ProductoId, request.DepositoId, ct);
            Guid? versionActual = context.Entry(saldo).State == EntityState.Added ? null : saldo.Version;
            if (versionActual != request.VersionOrigen)
                throw new BusinessException("Las existencias cambiaron desde tu consulta. Cerrá la ventana, actualizá el inventario y volvé a intentar.", 409);
            var destino = request.DepositoDestinoId.HasValue ? await ObtenerSaldoAsync(request.ProductoId, request.DepositoDestinoId.Value, ct) : null;
            var (nuevo, nuevoDestino) = InventarioReglas.Calcular(request.Tipo, request.Cantidad, saldo.Cantidad, destino?.Cantidad, producto.UnidadMedida);
            var movimiento = new MovimientoInventario
            {
                OperacionId = request.OperacionId, ProductoId = request.ProductoId, DepositoId = request.DepositoId,
                DepositoDestinoId = request.DepositoDestinoId, Tipo = request.Tipo, Cantidad = request.Cantidad,
                Variacion = nuevo - saldo.Cantidad, StockAnterior = saldo.Cantidad, StockPosterior = nuevo,
                StockDestinoAnterior = destino?.Cantidad, StockDestinoPosterior = nuevoDestino, Motivo = request.Motivo
            };
            saldo.Cantidad = nuevo;
            saldo.Version = Guid.NewGuid();
            if (destino is not null)
            {
                destino.Cantidad = nuevoDestino!.Value;
                destino.Version = Guid.NewGuid();
            }
            context.MovimientosInventario.Add(movimiento);
            // Compartir la versión del producto impide cambiar su unidad al mismo tiempo
            // que se registra su primer movimiento, y protege su estado activo.
            context.Productos.Attach(producto);
            producto.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            var result = await context.MovimientosInventario.AsNoTracking().Where(x => x.Id == movimiento.Id).Select(Proyeccion).SingleAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Otra operación modificó estas existencias. Actualizá y reintentá la misma operación; su identificador evita duplicados.", 409);
        }
    }

    private async Task<Existencia> ObtenerSaldoAsync(int productoId, int depositoId, CancellationToken ct)
    {
        var saldo = await context.Existencias.SingleOrDefaultAsync(x => x.ProductoId == productoId && x.DepositoId == depositoId, ct);
        if (saldo is not null) return saldo;
        saldo = new Existencia { ProductoId = productoId, DepositoId = depositoId };
        context.Existencias.Add(saldo);
        return saldo;
    }

    private static string Patron(string value) => "%" + value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    private static readonly Expression<Func<MovimientoInventario, MovimientoDto>> Proyeccion = x => new(
        x.Id, x.OperacionId, x.ProductoId, x.Producto.Codigo, x.Producto.Nombre, x.Producto.UnidadMedida,
        x.DepositoId, x.Deposito.Nombre, x.DepositoDestinoId, x.DepositoDestino == null ? null : x.DepositoDestino.Nombre,
        x.Tipo, x.Cantidad, x.Variacion, x.StockAnterior, x.StockPosterior, x.StockDestinoAnterior, x.StockDestinoPosterior,
        x.Motivo, x.CreadoEn, x.CreadoPor);
}
