using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Context.Presupuestos.Entity;
using NexaBack.Infrastructure.Data.Dtos.Presupuestos;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Repository.Ventas;
using NexaBack.Services.Presupuestos;
using NexaBack.Services.Ventas;
using Npgsql;
namespace NexaBack.Repository.Presupuestos;

public sealed class PresupuestoRepository(UsuariosDbContext context, IVentaRepository ventas) : IPresupuestoRepository
{
    public async Task<PresupuestosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        var hoy = PresupuestoReglas.Hoy;
        var q = DatosPresupuesto().AsNoTracking();
        if (estado == "Vencido") q = q.Where(x => (x.Estado == "Borrador" || x.Estado == "Aprobado") && x.FechaVencimiento < hoy);
        else if (estado is not null) q = q.Where(x => x.Estado == estado);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = Patron(buscar);
            q = q.Where(x => EF.Functions.ILike(x.Cliente.NombreRazonSocial, patron, "\\") || EF.Functions.ILike(x.Cliente.Documento, patron, "\\")
                || EF.Functions.ILike("PRE-" + x.Id.ToString().PadLeft(6, '0'), patron, "\\"));
        }
        var total = await q.CountAsync(ct);
        var filas = await q.OrderByDescending(x => x.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync(ct);
        return new(filas.Select(x => new PresupuestoResumenDto(x.Id, Numero(x.Id), x.Cliente.NombreRazonSocial, x.Cliente.TipoDocumento?.Nombre, x.Cliente.Documento,
            x.Deposito.Nombre, x.Estado, PresupuestoReglas.Vencido(x.Estado, x.FechaVencimiento, hoy), x.FechaVencimiento, x.Total, x.Version, x.CreadoEn, x.VentaId)).ToArray(), total, pagina, tamanoPagina);
    }
    public async Task<PresupuestoDto> ObtenerAsync(int id, CancellationToken ct) => Map(await Cargar(id, false, ct));

    public async Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct)
    {
        var cliente = await context.Clientes.AsNoTracking().Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == request.ClienteId, ct);
        if (cliente is null || !cliente.Activo) throw new BusinessException("El cliente no existe o está inactivo.");
        var deposito = await context.Depositos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.DepositoId, ct);
        if (deposito is null || !deposito.Activo) throw new BusinessException("El depósito no existe o está inactivo.");
        var ids = request.Items.Select(x => x.ProductoId).ToArray();
        var productos = await context.Productos.AsNoTracking().Include(x => x.UnidadMedidaCatalogo).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var saldos = await context.Existencias.AsNoTracking().Where(x => x.DepositoId == request.DepositoId && ids.Contains(x.ProductoId)).ToDictionaryAsync(x => x.ProductoId, ct);
        var lineas = new List<VentaCotizacionLinea>();
        foreach (var item in request.Items.OrderBy(x => x.ProductoId))
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Activo) throw new BusinessException("Un producto no existe o está inactivo.");
            // Presupuestar admite cantidades superiores al stock; no reserva ni genera salidas.
            var subtotal = VentaReglas.Subtotal(item.Cantidad, producto.PrecioVenta, producto.UnidadMedidaCatalogo.Codigo);
            lineas.Add(new(producto.Id, producto.Codigo, producto.Nombre, producto.UnidadMedidaCatalogo.Codigo, item.Cantidad, producto.PrecioVenta, subtotal,
                saldos.TryGetValue(producto.Id, out var saldo) ? saldo.Cantidad : 0, producto.Version));
        }
        var suma = lineas.Sum(x => x.Subtotal);
        return new(cliente.Id, cliente.NombreRazonSocial, deposito.Id, deposito.Nombre, suma, request.Descuento, VentaReglas.Total(suma, request.Descuento), lineas);
    }

    public async Task<PresupuestoDto> GuardarAsync(int? id, PresupuestoGuardarRequest request, string hash, CancellationToken ct)
    {
        try
        {
            return await Transaccion(async () =>
            {
                var usuario = Usuario();
                Presupuesto p;
                if (id.HasValue)
                {
                    p = await Cargar(id.Value, true, ct);
                    if (p.EdicionOperacionId == request.OperacionId) { Reintento(p.EdicionHash, hash, p.ModificadoPor, usuario); return Map(p); }
                    PresupuestoReglas.Version(p.Version, request.Version!.Value);
                    PresupuestoReglas.Editar(p.Estado);
                }
                else
                {
                    var anterior = await DatosPresupuesto(true).SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
                    if (anterior is not null) { Reintento(anterior.SolicitudHash, hash, anterior.CreadoPor, usuario); return Map(anterior); }
                    p = new Presupuesto { OperacionId = request.OperacionId, SolicitudHash = hash };
                    context.Presupuestos.Add(p);
                }
                PresupuestoReglas.Fecha(request.FechaVencimiento, PresupuestoReglas.Hoy);
                var calculo = await CotizarAsync(new(request.ClienteId, request.DepositoId, request.Descuento,
                    request.Items.Select(x => new VentaLineaSolicitud(x.ProductoId, x.Cantidad)).ToArray()), ct);
                foreach (var linea in calculo.Items)
                    if (request.Items.Single(x => x.ProductoId == linea.ProductoId).VersionProducto != linea.VersionProducto)
                        throw new BusinessException("Cambió un producto. Calculá nuevamente y revisá los precios antes de guardar.", 409);
                p.ClienteId = request.ClienteId;
                p.DepositoId = calculo.DepositoId; p.FechaVencimiento = request.FechaVencimiento;
                p.Observaciones = request.Observaciones; p.Subtotal = calculo.Subtotal; p.Descuento = calculo.Descuento; p.Total = calculo.Total; p.Version = Guid.NewGuid();
                if (id.HasValue) { p.EdicionOperacionId = request.OperacionId; p.EdicionHash = hash; }
                // Conservar las líneas existentes mantiene su auditoría original al editar el borrador.
                foreach (var vieja in p.Detalles.Where(x => !request.Items.Any(i => i.ProductoId == x.ProductoId)).ToArray())
                {
                    context.PresupuestosDetalles.Remove(vieja); p.Detalles.Remove(vieja);
                }
                foreach (var linea in calculo.Items)
                {
                    var detalle = p.Detalles.SingleOrDefault(x => x.ProductoId == linea.ProductoId);
                    if (detalle is null) { detalle = new PresupuestoDetalle { ProductoId = linea.ProductoId, UnidadMedida = "" }; p.Detalles.Add(detalle); }
                    detalle.UnidadMedida = linea.UnidadMedida;
                    detalle.Cantidad = linea.Cantidad; detalle.PrecioUnitario = linea.PrecioUnitario; detalle.Subtotal = linea.Subtotal;
                }
                await context.SaveChangesAsync(ct);
                return await ObtenerAsync(p.Id, ct);
            }, ct);
        }
        catch (BusinessException ex) when (ex.StatusCode == 409)
        {
            context.ChangeTracker.Clear();
            var p = id.HasValue ? await DatosPresupuesto(true).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
                : await DatosPresupuesto(true).AsNoTracking().SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (p is null || id.HasValue && p.EdicionOperacionId != request.OperacionId) throw;
            Reintento(id.HasValue ? p.EdicionHash : p.SolicitudHash, hash, id.HasValue ? p.ModificadoPor : p.CreadoPor, Usuario());
            return Map(p);
        }
    }

    public Task<PresupuestoDto> AprobarAsync(int id, Guid version, CancellationToken ct) => Transaccion(async () =>
    {
        Usuario(); var p = await Cargar(id, true, ct);
        if (p.Estado == "Aprobado") return Map(p);
        PresupuestoReglas.Version(p.Version, version);
        PresupuestoReglas.Aprobar(p.Estado, p.FechaVencimiento, PresupuestoReglas.Hoy);
        p.Estado = "Aprobado"; p.AprobadoEn = DateTime.UtcNow; p.AprobadoPor = Usuario(); p.Version = Guid.NewGuid();
        await context.SaveChangesAsync(ct); return Map(p);
    }, ct);
    public Task<PresupuestoDto> CancelarAsync(int id, PresupuestoCancelarRequest request, CancellationToken ct) => Transaccion(async () =>
    {
        Usuario(); var p = await Cargar(id, true, ct);
        if (p.Estado == "Cancelado")
        {
            if (p.MotivoCancelacion != request.Motivo) throw new BusinessException("El presupuesto ya se canceló con otro motivo.", 409);
            return Map(p);
        }
        PresupuestoReglas.Version(p.Version, request.Version); PresupuestoReglas.Cancelar(p.Estado);
        p.Estado = "Cancelado"; p.CanceladoEn = DateTime.UtcNow; p.MotivoCancelacion = request.Motivo; p.Version = Guid.NewGuid();
        await context.SaveChangesAsync(ct); return Map(p);
    }, ct);

    public async Task<PresupuestoConversionDto> PrepararConversionAsync(int id, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var p = await Cargar(id, false, ct);
        PresupuestoReglas.Convertir(p.Estado, p.FechaVencimiento, PresupuestoReglas.Hoy);
        var calculo = await ventas.CotizarAsync(new(p.ClienteId, p.DepositoId, p.Descuento,
            p.Detalles.Select(x => new VentaLineaSolicitud(x.ProductoId, x.Cantidad)).ToArray()), ct);
        await tx.CommitAsync(ct); return new(p.Version, calculo);
    }
    public async Task<VentaDto> ConvertirAsync(int id, PresupuestoConvertirRequest request, string hash, CancellationToken ct)
    {
        try
        {
            return await Transaccion(async () =>
            {
                Usuario(); var p = await Cargar(id, true, ct);
                if (p.VentaId.HasValue) return await RecuperarVenta(p, request, hash, ct);
                PresupuestoReglas.Version(p.Version, request.Version);
                PresupuestoReglas.Convertir(p.Estado, p.FechaVencimiento, PresupuestoReglas.Hoy);
                if (request.Items.Count != p.Detalles.Count || p.Detalles.Any(x => !request.Items.Any(i => i.ProductoId == x.ProductoId)))
                    throw new BusinessException("Los productos no coinciden con el presupuesto aprobado.", 409);
                var datos = new VentaConfirmarRequest(request.OperacionId, p.ClienteId, p.DepositoId, p.Descuento, p.Observaciones,
                    p.Detalles.OrderBy(x => x.ProductoId).Select(x => new VentaConfirmarLinea(x.ProductoId, x.Cantidad,
                        request.Items.Single(i => i.ProductoId == x.ProductoId).VersionProducto)).ToArray());
                // Venta, stock y vínculo al presupuesto comparten la misma transacción.
                // Los precios actuales deben coincidir con las versiones que el usuario acaba de revisar.
                var venta = await ventas.ConfirmarAsync(datos, hash, ct);
                p.VentaId = venta.Id; p.Estado = "Convertido"; p.Version = Guid.NewGuid();
                await context.SaveChangesAsync(ct); return venta;
            }, ct);
        }
        catch (BusinessException ex) when (ex.StatusCode == 409)
        {
            context.ChangeTracker.Clear();
            var p = await context.Presupuestos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (p?.VentaId is null) throw;
            return await RecuperarVenta(p, request, hash, ct);
        }
    }
    private async Task<VentaDto> RecuperarVenta(Presupuesto p, PresupuestoConvertirRequest request, string hash, CancellationToken ct)
    {
        var v = await context.Ventas.AsNoTracking().SingleAsync(x => x.Id == p.VentaId, ct);
        if (v.OperacionId != request.OperacionId) throw new BusinessException("El presupuesto ya fue convertido. Consultá su venta asociada.", 409);
        Reintento(v.SolicitudHash, hash, v.CreadoPor, Usuario());
        return await ventas.ObtenerAsync(v.Id, ct);
    }
    private async Task<Presupuesto> Cargar(int id, bool seguimiento, CancellationToken ct)
    {
        var q = DatosPresupuesto(true).AsQueryable();
        if (!seguimiento) q = q.AsNoTracking();
        return await q.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Presupuesto no encontrado.", 404);
    }
    private static void Reintento(string? anterior, string hash, int? autor, int usuario)
    {
        if (anterior != hash || autor != usuario) throw new BusinessException("El identificador de operación ya se utilizó con otros datos.", 409);
    }
    private int Usuario() => context.UsuarioAuditoriaId ?? throw new BusinessException("Se requiere una sesión válida.", 401);
    private async Task<T> Transaccion<T>(Func<Task<T>> accion, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try { var resultado = await accion(); await tx.CommitAsync(ct); return resultado; }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear(); throw new BusinessException("Otra operación modificó los datos. Actualizá y revisá antes de continuar. Conservá el UUID si estás comprobando un reintento.", 409);
        }
    }
    private static string Numero(int id) => "PRE-" + id.ToString("D6");
    private static string Patron(string texto) => "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    // Los nombres se leen de sus tablas por ID; el listado no necesita cargar las líneas.
    private IQueryable<Presupuesto> DatosPresupuesto(bool incluirDetalles = false)
    {
        var consulta = context.Presupuestos.Include(x => x.Cliente).ThenInclude(x => x.TipoDocumento).Include(x => x.Deposito);
        return incluirDetalles ? consulta.Include(x => x.Detalles).ThenInclude(x => x.Producto) : consulta;
    }

    private static PresupuestoDto Map(Presupuesto x) => new(x.Id, Numero(x.Id), x.ClienteId, x.Cliente.NombreRazonSocial, x.Cliente.TipoDocumento?.Nombre, x.Cliente.Documento,
        x.DepositoId, x.Deposito.Nombre, x.Estado, PresupuestoReglas.Vencido(x.Estado, x.FechaVencimiento, PresupuestoReglas.Hoy), x.FechaVencimiento, x.Observaciones,
        x.Subtotal, x.Descuento, x.Total, x.Version, x.AprobadoEn, x.AprobadoPor, x.CanceladoEn, x.MotivoCancelacion, x.VentaId,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor, x.Detalles.OrderBy(d => d.Id).Select(d => new PresupuestoLineaDto(d.Id, d.ProductoId, d.Producto.Codigo, d.Producto.Nombre,
            d.UnidadMedida, d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToArray());
}
