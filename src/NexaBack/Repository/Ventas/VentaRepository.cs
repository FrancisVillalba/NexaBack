using NexaBack.Services.Cobros;
using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Ventas.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Services.Ventas;
using NexaBack.Services.Inventario;
using Npgsql;
namespace NexaBack.Repository.Ventas;

public sealed class VentaRepository(UsuariosDbContext context) : IVentaRepository
{
    public async Task<VentasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        var query = context.Ventas.AsNoTracking();
        if (estado is not null) query = query.Where(x => x.Estado == estado);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = Patron(buscar);
            query = query.Where(x => EF.Functions.ILike(x.ClienteNombre, patron, "\\")
                || EF.Functions.ILike(x.ClienteDocumento, patron, "\\") || EF.Functions.ILike(x.DepositoNombre, patron, "\\")
                || EF.Functions.ILike("VTA-" + x.Id.ToString().PadLeft(6, '0'), patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        var filas = await query.OrderByDescending(x => x.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync(ct);
        return new(filas.Select(x => new VentaResumenDto(x.Id, Numero(x.Id), x.ClienteNombre, x.ClienteTipoDocumento,
            x.ClienteDocumento, x.DepositoNombre, x.Estado, x.Total, x.Version, x.CreadoEn)).ToArray(), total, pagina, tamanoPagina);
    }
    public async Task<VentaDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await context.Ventas.AsNoTracking().Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Venta no encontrada.", 404));

    public async Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct)
    {
        var cliente = await context.Clientes.AsNoTracking().Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == request.ClienteId, ct);
        if (cliente is null || !cliente.Activo) throw new BusinessException("El cliente no existe o está inactivo.");
        var deposito = await context.Depositos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.DepositoId, ct);
        if (deposito is null || !deposito.Activo) throw new BusinessException("El depósito no existe o está inactivo.");
        var ids = request.Items.Select(x => x.ProductoId).ToArray();
        var productos = await context.Productos.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var saldos = await context.Existencias.AsNoTracking().Where(x => x.DepositoId == request.DepositoId && ids.Contains(x.ProductoId)).ToDictionaryAsync(x => x.ProductoId, ct);
        var lineas = new List<VentaCotizacionLinea>();
        foreach (var item in request.Items.OrderBy(x => x.ProductoId))
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Activo) throw new BusinessException("Un producto no existe o está inactivo.");
            var disponible = saldos.TryGetValue(item.ProductoId, out var saldo) ? saldo.Cantidad : 0;
            InventarioReglas.Calcular("salida", item.Cantidad, disponible, null, producto.UnidadMedida);
            var subtotal = VentaReglas.Subtotal(item.Cantidad, producto.PrecioVenta, producto.UnidadMedida);
            lineas.Add(new(producto.Id, producto.Codigo, producto.Nombre, producto.UnidadMedida, item.Cantidad,
                producto.PrecioVenta, subtotal, disponible, producto.Version));
        }
        var suma = lineas.Sum(x => x.Subtotal);
        return new(cliente.Id, cliente.NombreRazonSocial, deposito.Id, deposito.Nombre, suma, request.Descuento,
            VentaReglas.Total(suma, request.Descuento), lineas);
    }

    public async Task<VentaDto> ConfirmarAsync(VentaConfirmarRequest request, string hash, CancellationToken ct)
    {
        // Presupuestos controla el commit conjunto de venta, stock y vínculo.
        // No recuperar conflictos dentro de una transacción ajena: PostgreSQL puede haberla invalidado.
        if (context.Database.CurrentTransaction is not null) return await ConfirmarTransaccionAsync(request, hash, ct);
        try { return await ConfirmarTransaccionAsync(request, hash, ct); }
        catch (BusinessException ex) when (ex.StatusCode == 409)
        {
            // Otro reintento pudo confirmar el mismo UUID mientras esta transacción esperaba.
            // La transacción anterior ya se descartó: consultar el resultado confirmado sin repetir salidas.
            context.ChangeTracker.Clear();
            var existente = await context.Ventas.AsNoTracking().Include(x => x.Detalles).SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (existente is null) throw;
            if (existente.SolicitudHash != hash || existente.CreadoPor != Usuario())
                throw new BusinessException("Ese identificador de operación ya se utilizó con otros datos.", 409);
            return Map(existente);
        }
    }

    private Task<VentaDto> ConfirmarTransaccionAsync(VentaConfirmarRequest request, string hash, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            var usuario = Usuario();
            var anterior = await context.Ventas.Include(x => x.Detalles).SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (anterior is not null)
            {
                if (anterior.SolicitudHash != hash || anterior.CreadoPor != usuario) throw new BusinessException("Ese identificador de operación ya se utilizó con otros datos.", 409);
                return Map(anterior);
            }
            var cliente = await context.Clientes.Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == request.ClienteId, ct);
            if (cliente is null || !cliente.Activo) throw new BusinessException("El cliente no existe o está inactivo.");
            var deposito = await context.Depositos.SingleOrDefaultAsync(x => x.Id == request.DepositoId, ct);
            if (deposito is null || !deposito.Activo) throw new BusinessException("El depósito no existe o está inactivo.");
            var ids = request.Items.Select(x => x.ProductoId).ToArray();
            var productos = await context.Productos.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var saldos = await context.Existencias.Where(x => x.DepositoId == request.DepositoId && ids.Contains(x.ProductoId)).ToDictionaryAsync(x => x.ProductoId, ct);
            var venta = new Venta { OperacionId = request.OperacionId, SolicitudHash = hash, ClienteId = cliente.Id,
                ClienteNombre = cliente.NombreRazonSocial, ClienteTipoDocumento = cliente.TipoDocumento == null ? null : cliente.TipoDocumento.Nombre, ClienteDocumento = cliente.Documento,
                DepositoId = deposito.Id, DepositoNombre = deposito.Nombre, Descuento = request.Descuento, Observaciones = request.Observaciones };
            foreach (var item in request.Items)
            {
                if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Activo) throw new BusinessException("Un producto no existe o está inactivo.");
                if (producto.Version != item.VersionProducto) throw new BusinessException("Cambió el precio, estado o stock de un producto. Volvé a calcular el total y revisalo antes de confirmar.", 409);
                if (!saldos.TryGetValue(item.ProductoId, out var saldo)) throw new BusinessException("No hay existencias para un producto.", 409);
                var (nuevo, _) = InventarioReglas.Calcular("salida", item.Cantidad, saldo.Cantidad, null, producto.UnidadMedida);
                var subtotal = VentaReglas.Subtotal(item.Cantidad, producto.PrecioVenta, producto.UnidadMedida);
                var movimiento = new MovimientoInventario { OperacionId = Guid.NewGuid(), ProductoId = producto.Id, DepositoId = deposito.Id,
                    Tipo = "salida", Cantidad = item.Cantidad, Variacion = -item.Cantidad, StockAnterior = saldo.Cantidad, StockPosterior = nuevo,
                    Motivo = "Venta (operación " + request.OperacionId + ")" };
                // La salida, el saldo y la venta se persisten juntos. El detalle referencia el movimiento.
                context.MovimientosInventario.Add(movimiento);
                venta.Detalles.Add(new VentaDetalle { ProductoId = producto.Id, ProductoCodigo = producto.Codigo,
                    ProductoNombre = producto.Nombre, UnidadMedida = producto.UnidadMedida, Cantidad = item.Cantidad,
                    PrecioUnitario = producto.PrecioVenta, CostoUnitario = producto.Costo, Subtotal = subtotal, MovimientoSalida = movimiento });
                venta.Subtotal += subtotal;
                saldo.Cantidad = nuevo;
                saldo.Version = Guid.NewGuid();
                producto.Version = Guid.NewGuid();
            }
            venta.Total = VentaReglas.Total(venta.Subtotal, venta.Descuento);
            cliente.Version = Guid.NewGuid();
            deposito.Version = Guid.NewGuid();
            context.Ventas.Add(venta);
            await context.SaveChangesAsync(ct);
            return Map(venta);
        }, ct);

    public Task<VentaDto> AnularAsync(int id, VentaAnularRequest request, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var venta = await context.Ventas.Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new BusinessException("Venta no encontrada.", 404);
            if (venta.Estado == "Anulada")
            {
                if (venta.MotivoAnulacion != request.Motivo) throw new BusinessException("La venta ya fue anulada con otro motivo. Actualizá su detalle.", 409);
                return Map(venta);
            }
            VentaReglas.Version(venta.Version, request.Version);
            CobroReglas.PermitirAnularVenta(await context.Cobros.AnyAsync(x => x.VentaId == venta.Id && x.Estado == "Vigente", ct));
            if (venta.Estado != "Confirmada" || venta.Detalles.Count == 0) throw new BusinessException("La venta no admite anulación.", 409);
            var deposito = await context.Depositos.SingleAsync(x => x.Id == venta.DepositoId, ct);
            if (!deposito.Activo) throw new BusinessException("Reactivá el depósito original antes de devolverle las existencias.", 409);
            var ids = venta.Detalles.Select(x => x.ProductoId).ToArray();
            var productos = await context.Productos.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var saldos = await context.Existencias.Where(x => x.DepositoId == venta.DepositoId && ids.Contains(x.ProductoId)).ToDictionaryAsync(x => x.ProductoId, ct);
            foreach (var item in venta.Detalles.OrderBy(x => x.ProductoId))
            {
                if (item.MovimientoAnulacionId is not null) throw new BusinessException("La línea ya tiene una devolución asociada.", 409);
                if (!productos.TryGetValue(item.ProductoId, out var producto) || producto.UnidadMedida != item.UnidadMedida)
                    throw new BusinessException("El producto no existe o cambió de unidad. Revisá el inventario.", 409);
                // Una anulación devuelve los artículos originales, aunque el producto o cliente ya esté inactivo.
                if (!saldos.TryGetValue(item.ProductoId, out var saldo))
                {
                    saldo = new Existencia { ProductoId = item.ProductoId, DepositoId = venta.DepositoId };
                    context.Existencias.Add(saldo);
                }
                var (nuevo, _) = InventarioReglas.Calcular("entrada", item.Cantidad, saldo.Cantidad, null, item.UnidadMedida);
                item.MovimientoAnulacion = new MovimientoInventario { OperacionId = Guid.NewGuid(), ProductoId = item.ProductoId,
                    DepositoId = venta.DepositoId, Tipo = "entrada", Cantidad = item.Cantidad, Variacion = item.Cantidad,
                    StockAnterior = saldo.Cantidad, StockPosterior = nuevo, Motivo = "Anulación de venta " + Numero(venta.Id) };
                context.MovimientosInventario.Add(item.MovimientoAnulacion);
                saldo.Cantidad = nuevo;
                saldo.Version = Guid.NewGuid();
                producto.Version = Guid.NewGuid();
            }
            deposito.Version = Guid.NewGuid();
            venta.Estado = "Anulada";
            venta.AnuladaEn = DateTime.UtcNow;
            venta.MotivoAnulacion = request.Motivo;
            venta.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return Map(venta);
        }, ct);

    public async Task<IReadOnlyList<VentaClienteDto>> ClientesAsync(string buscar, CancellationToken ct)
    {
        var patron = Patron(buscar);
        return await context.Clientes.AsNoTracking().Where(x => x.Activo && (EF.Functions.ILike(x.NombreRazonSocial, patron, "\\") || EF.Functions.ILike(x.Documento, patron, "\\")))
            .OrderBy(x => x.NombreRazonSocial).ThenBy(x => x.Id).Take(20).Select(x => new VentaClienteDto(x.Id, x.NombreRazonSocial, x.TipoDocumento == null ? null : x.TipoDocumento.Nombre, x.Documento)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<VentaDepositoDto>> DepositosAsync(CancellationToken ct)
        => await context.Depositos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new VentaDepositoDto(x.Id, x.Nombre)).ToListAsync(ct);
    public async Task<IReadOnlyList<VentaProductoDto>> ProductosAsync(int depositoId, string buscar, CancellationToken ct)
    {
        if (!await context.Depositos.AnyAsync(x => x.Id == depositoId && x.Activo, ct)) throw new BusinessException("Depósito no encontrado o inactivo.");
        var patron = Patron(buscar);
        return await context.Productos.AsNoTracking().Where(x => x.Activo && (EF.Functions.ILike(x.Nombre, patron, "\\")
            || EF.Functions.ILike(x.Codigo, patron, "\\") || EF.Functions.ILike(x.CodigoBarras ?? "", patron, "\\")))
            .OrderBy(x => x.Nombre).ThenBy(x => x.Id).Take(20).Select(x => new VentaProductoDto(x.Id, x.Codigo, x.Nombre, x.UnidadMedida, x.PrecioVenta,
                context.Existencias.Where(e => e.DepositoId == depositoId && e.ProductoId == x.Id).Select(e => (decimal?)e.Cantidad).FirstOrDefault() ?? 0m)).ToListAsync(ct);
    }
    private int Usuario() => context.UsuarioAuditoriaId ?? throw new BusinessException("Se requiere una sesión válida.", 401);
    private async Task<VentaDto> TransaccionAsync(Func<Task<VentaDto>> accion, CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is not null) return await accion();
        // Evita ventas simultáneas sobre el mismo saldo y anulación/confirmación duplicadas.
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try { var resultado = await accion(); await transaccion.CommitAsync(ct); return resultado; }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Otra operación modificó estos datos. Reintentá la misma confirmación; si el conflicto continúa, recalculá el total. El identificador evita duplicar la venta.", 409);
        }
    }
    private static string Numero(int id) => $"VTA-{id:D6}";
    private static string Patron(string texto) => "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static VentaDto Map(Venta x) => new(x.Id, Numero(x.Id), x.ClienteId, x.ClienteNombre, x.ClienteTipoDocumento, x.ClienteDocumento,
        x.DepositoId, x.DepositoNombre, x.Estado, x.Observaciones, x.Subtotal, x.Descuento, x.Total, x.Version, x.AnuladaEn, x.MotivoAnulacion,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor, x.Detalles.OrderBy(d => d.Id).Select(d => new VentaLineaDto(d.Id, d.ProductoId,
            d.ProductoCodigo, d.ProductoNombre, d.UnidadMedida, d.Cantidad, d.PrecioUnitario, d.Subtotal, d.MovimientoSalidaId, d.MovimientoAnulacionId)).ToArray());
}
