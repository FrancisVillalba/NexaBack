using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Compras.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.Compras;
using NexaBack.Services.Compras;
using NexaBack.Services.Inventario;
using Npgsql;

namespace NexaBack.Repository.Compras;

public sealed class CompraRepository(UsuariosDbContext context) : ICompraRepository
{
    public async Task<ComprasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        var query = context.Compras.AsNoTracking();
        if (estado is not null) query = query.Where(x => x.Estado == estado);
        if (!string.IsNullOrEmpty(buscar))
        {
            var patron = Patron(buscar);
            query = query.Where(x => EF.Functions.ILike(x.ProveedorNombre, patron, "\\")
                || EF.Functions.ILike(x.ProveedorDocumento ?? "", patron, "\\")
                || EF.Functions.ILike(x.DepositoNombre, patron, "\\") || EF.Functions.ILike("OC-" + x.Id.ToString().PadLeft(6, '0'), patron, "\\"));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Id).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync(ct);
        return new(items.Select(x => new CompraResumenDto(x.Id, Numero(x.Id), x.ProveedorNombre, x.ProveedorTipoDocumento, x.ProveedorDocumento,
            x.DepositoNombre, x.Estado, x.Total, x.Version, x.CreadoEn)).ToArray(), total, pagina, tamanoPagina);
    }

    public async Task<CompraDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await context.Compras.AsNoTracking().Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Compra no encontrada.", 404));

    public Task<CompraDto> CrearAsync(CrearCompraRequest request, string solicitudHash, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            var usuario = Usuario();
            var anterior = await context.Compras.Include(x => x.Detalles).SingleOrDefaultAsync(x => x.OperacionId == request.OperacionId, ct);
            if (anterior is not null)
            {
                if (anterior.SolicitudHash != solicitudHash || anterior.CreadoPor != usuario)
                    throw new BusinessException("El identificador de operación ya se utilizó con otros datos.", 409);
                return Map(anterior);
            }
            var proveedor = await context.Proveedores.Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == request.ProveedorId, ct);
            if (proveedor is null || !proveedor.Activo) throw new BusinessException("El proveedor no existe o está inactivo.");
            var deposito = await context.Depositos.SingleOrDefaultAsync(x => x.Id == request.DepositoId, ct);
            if (deposito is null || !deposito.Activo) throw new BusinessException("El depósito no existe o está inactivo.");
            var ids = request.Items.Select(x => x.ProductoId).ToArray();
            var productos = await context.Productos.Include(x => x.UnidadMedidaCatalogo).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var compra = new Compra {
                OperacionId = request.OperacionId, SolicitudHash = solicitudHash, ProveedorId = proveedor.Id,
                ProveedorNombre = proveedor.NombreRazonSocial,
                ProveedorTipoDocumento = proveedor.TipoDocumento == null ? null : proveedor.TipoDocumento.Nombre, ProveedorDocumento = proveedor.Documento, DepositoId = deposito.Id, DepositoNombre = deposito.Nombre,
                Observaciones = request.Observaciones
            };
            foreach (var item in request.Items)
            {
                if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Activo) throw new BusinessException("Un producto no existe o está inactivo.");
                var subtotal = CompraReglas.Subtotal(item.Cantidad, item.CostoUnitario, producto.UnidadMedidaCatalogo.Codigo);
                compra.Detalles.Add(new CompraDetalle { ProductoId = producto.Id, ProductoCodigo = producto.Codigo,
                    ProductoNombre = producto.Nombre, UnidadMedida = producto.UnidadMedidaCatalogo.Codigo,
                    Cantidad = item.Cantidad, CostoUnitario = item.CostoUnitario, Subtotal = subtotal });
                compra.Total += subtotal;
            }
            if (compra.Total > CompraReglas.MaximoImporte) throw new BusinessException("El total de la compra supera el máximo permitido.");
            context.Compras.Add(compra);
            await context.SaveChangesAsync(ct);
            return Map(compra);
        }, ct);

    public Task<CompraDto> RecibirAsync(int id, Guid version, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var compra = await CargarAsync(id, ct);
            // Reintentar una recepción confirmada devuelve la compra sin volver a sumar stock.
            if (compra.Estado == "Recibida") return Map(compra);
            CompraReglas.ValidarVersion(compra.Version, version);
            if (compra.Estado != "Pendiente") throw new BusinessException("Solo se reciben compras pendientes.", 409);
            var proveedor = await context.Proveedores.Include(x => x.TipoDocumento).SingleAsync(x => x.Id == compra.ProveedorId, ct);
            if (!proveedor.Activo) throw new BusinessException("El proveedor está inactivo.");
            var deposito = await context.Depositos.SingleAsync(x => x.Id == compra.DepositoId, ct);
            if (!deposito.Activo) throw new BusinessException("El depósito está inactivo.");
            if (compra.Detalles.Count == 0) throw new BusinessException("La compra no tiene productos.");
            var ids = compra.Detalles.Select(x => x.ProductoId).ToArray();
            var productos = await context.Productos.Include(x => x.UnidadMedidaCatalogo).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var saldos = await context.Existencias.Where(x => x.DepositoId == compra.DepositoId && ids.Contains(x.ProductoId)).ToDictionaryAsync(x => x.ProductoId, ct);
            foreach (var item in compra.Detalles.OrderBy(x => x.ProductoId))
            {
                if (item.MovimientoId is not null) throw new BusinessException("La línea ya tiene un movimiento asociado.", 409);
                if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Activo || producto.UnidadMedidaCatalogo.Codigo != item.UnidadMedida)
                    throw new BusinessException("Un producto está inactivo o cambió su unidad. Revisá la compra antes de recibirla.", 409);
                if (!saldos.TryGetValue(item.ProductoId, out var saldo))
                {
                    saldo = new Existencia { ProductoId = item.ProductoId, DepositoId = compra.DepositoId };
                    context.Existencias.Add(saldo);
                }
                var (nuevo, _) = InventarioReglas.Calcular("entrada", item.Cantidad, saldo.Cantidad, null, item.UnidadMedida);
                item.Movimiento = new MovimientoInventario { OperacionId = Guid.NewGuid(), ProductoId = item.ProductoId,
                    DepositoId = compra.DepositoId, Tipo = "entrada", Cantidad = item.Cantidad, Variacion = item.Cantidad,
                    StockAnterior = saldo.Cantidad, StockPosterior = nuevo, Motivo = "Recepción de compra " + Numero(compra.Id) };
                context.MovimientosInventario.Add(item.Movimiento);
                // EF inserta movimiento y enlace junto con todos los saldos en un solo SaveChanges.
                saldo.Cantidad = nuevo;
                saldo.Version = Guid.NewGuid();
                producto.Version = Guid.NewGuid();
            }
            deposito.Version = Guid.NewGuid();
            proveedor.Version = Guid.NewGuid();
            compra.Estado = "Recibida";
            compra.RecibidoEn = DateTime.UtcNow;
            compra.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return Map(compra);
        }, ct);

    public Task<CompraDto> CancelarAsync(int id, Guid version, CancellationToken ct)
        => TransaccionAsync(async () =>
        {
            Usuario();
            var compra = await CargarAsync(id, ct);
            if (compra.Estado == "Cancelada") return Map(compra);
            CompraReglas.ValidarVersion(compra.Version, version);
            if (compra.Estado != "Pendiente") throw new BusinessException("Solo se cancelan compras pendientes. Una compra recibida conserva su historial y movimientos.", 409);
            compra.Estado = "Cancelada";
            compra.Version = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return Map(compra);
        }, ct);

    // Catálogos mínimos para Compras: no requieren abrir los ABM administrativos.
    public async Task<IReadOnlyList<CompraProveedorDto>> ProveedoresAsync(string buscar, CancellationToken ct)
    {
        var patron = Patron(buscar);
        return await context.Proveedores.AsNoTracking().Where(x => x.Activo
            && (EF.Functions.ILike(x.NombreRazonSocial, patron, "\\") || EF.Functions.ILike(x.Documento ?? "", patron, "\\")))
            .OrderBy(x => x.NombreRazonSocial).ThenBy(x => x.Id).Take(20)
            .Select(x => new CompraProveedorDto(x.Id, x.NombreRazonSocial, x.TipoDocumento == null ? null : x.TipoDocumento.Nombre, x.Documento)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<CompraProductoDto>> ProductosAsync(string buscar, CancellationToken ct)
    {
        var patron = Patron(buscar);
        return await context.Productos.AsNoTracking().Include(x => x.UnidadMedidaCatalogo).Where(x => x.Activo && (EF.Functions.ILike(x.Nombre, patron, "\\")
            || EF.Functions.ILike(x.Codigo, patron, "\\") || EF.Functions.ILike(x.CodigoBarras ?? "", patron, "\\")))
            .OrderBy(x => x.Nombre).ThenBy(x => x.Id).Take(20)
            .Select(x => new CompraProductoDto(x.Id, x.Codigo, x.Nombre, x.UnidadMedidaCatalogo.Codigo, x.Costo)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<CompraDepositoDto>> DepositosAsync(CancellationToken ct)
        => await context.Depositos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new CompraDepositoDto(x.Id, x.Nombre)).ToListAsync(ct);

    private int Usuario() => context.UsuarioAuditoriaId ?? throw new BusinessException("Se requiere una sesión válida.", 401);
    private async Task<Compra> CargarAsync(int id, CancellationToken ct) => await context.Compras.Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new BusinessException("Compra no encontrada.", 404);
    // Serializable y versiones protegen recepción/cancelación simultáneas y la primera existencia.
    private async Task<CompraDto> TransaccionAsync(Func<Task<CompraDto>> accion, CancellationToken ct)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var resultado = await accion();
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException
            || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation }
            || ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } })
        {
            context.ChangeTracker.Clear();
            throw new BusinessException("Otra operación modificó estos datos. Actualizá y reintentá; una recepción confirmada no duplica existencias.", 409);
        }
    }
    private static string Numero(int id) => $"OC-{id:D6}";
    private static string Patron(string texto) => "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    private static CompraDto Map(Compra x) => new(x.Id, Numero(x.Id), x.ProveedorId, x.ProveedorNombre, x.ProveedorTipoDocumento, x.ProveedorDocumento,
        x.DepositoId, x.DepositoNombre, x.Estado, x.Observaciones, x.Total, x.Version, x.RecibidoEn,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor, x.Detalles.OrderBy(d => d.Id).Select(d => new CompraLineaDto(
            d.Id, d.ProductoId, d.ProductoCodigo, d.ProductoNombre, d.UnidadMedida, d.Cantidad, d.CostoUnitario, d.Subtotal, d.MovimientoId)).ToArray());
}
