using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.Reportes;
using NexaBack.Services.Reportes;
namespace NexaBack.Repository.Reportes;

// Solo consultas: no guarda saldos duplicados, indicadores ni copias de las operaciones.
public sealed class ReportesRepository(UsuariosDbContext context) : IReportesRepository
{
    public async Task<IReadOnlyList<ReporteDepositoDto>> DepositosAsync(CancellationToken ct)
        => await context.Depositos.AsNoTracking().OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Select(x => new ReporteDepositoDto(x.Id, x.Nombre, x.Activo)).ToListAsync(ct);
    private async Task<string> DepositoNombre(int? id, CancellationToken ct)
        => id.HasValue ? (await context.Depositos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("Depósito no encontrado.", 404)).Nombre : "Todos los depósitos";

    public async Task<ReporteDto> ConsultarAsync(ReporteConsulta consulta, bool exportar, CancellationToken ct)
    {
        // Totales y páginas/exportación pertenecen a la misma instantánea, aunque otro usuario esté cobrando.
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var nombre = await DepositoNombre(consulta.DepositoId, ct);
        var query = Filas(consulta);
        var resumen = await Resumen(query, ct);
        if (exportar) ReportesReglas.Exportacion(resumen.Registros);
        var pagina = exportar ? 1 : consulta.Pagina;
        var tamano = exportar ? ReportesReglas.LimiteExportacion : consulta.TamanoPagina;
        var filas = await query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        var resultado = new ReporteDto(consulta.Tipo, consulta.Desde, consulta.Hasta, consulta.DepositoId, nombre, DateTime.UtcNow,
            ReportesReglas.UsaPeriodo(consulta.Tipo), resumen, filas, pagina, tamano,
            consulta.Tipo == "ventas" ? await DetalleVentas(consulta, resumen, ct) : null,
            consulta.Tipo == "inventario" ? await Categorias(consulta.DepositoId, ct) : Array.Empty<ReporteCategoriaDto>());
        await tx.CommitAsync(ct); return resultado;
    }
    public async Task<InicioDto> InicioAsync(int? depositoId, IReadOnlySet<string> permisos, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var nombre = await DepositoNombre(depositoId, ct);
        var hoy = ReportesReglas.Hoy; var desde = new DateOnly(hoy.Year, hoy.Month, 1);
        var indicadores = new List<InicioIndicadorDto>();
        var ultimas = new List<ReporteFilaDto>(); var bajos = new List<ReporteFilaDto>();
        IReadOnlyList<ReporteDiaDto> dias = Array.Empty<ReporteDiaDto>();
        IReadOnlyList<ReporteCategoriaDto> categorias = Array.Empty<ReporteCategoriaDto>();
        foreach (var tipo in ReportesReglas.Tipos)
        {
            if (!permisos.Contains(ReportesReglas.Permiso(tipo))) continue;
            var consulta = new ReporteConsulta(tipo, desde, hoy, depositoId);
            var filas = Filas(consulta); var resumen = await Resumen(filas, ct);
            var (titulo, ruta) = tipo switch
            {
                "ventas" => ("Ventas del mes", "ventas"),
                "compras" => ("Compras recibidas del mes", "compras"),
                "cobros" => ("Cobros del mes", "ventas"),
                "pagos" => ("Pagos del mes", "compras"),
                "por-cobrar" => ("Saldo por cobrar actual", "clientes"),
                "por-pagar" => ("Saldo por pagar actual", "proveedores"),
                _ => ("Inventario al costo actual", "inventario")
            };
            indicadores.Add(new(tipo, titulo, tipo is "por-cobrar" or "por-pagar" ? resumen.Saldo : resumen.Importe, resumen.Registros,
                ReportesReglas.UsaPeriodo(tipo) ? "Del inicio del mes hasta hoy · operaciones vigentes" : "Situación actual · todos los períodos", ruta));
            if (tipo == "ventas")
            {
                dias = await Dias(new ReporteConsulta("ventas", hoy.AddDays(-6), hoy, depositoId), ct);
                // Últimas ventas de cualquier fecha, no solo las del mes.
                var historico = Filas(consulta with { Desde = DateOnly.MinValue, Hasta = new DateOnly(9998, 12, 31) });
                ultimas = await historico.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).Take(6).ToListAsync(ct);
            }
            if (tipo == "inventario")
            {
                categorias = await Categorias(depositoId, ct);
                var stock = Inventario(depositoId, true);
                bajos = await stock.Where(x => x.Cantidad <= x.Minimo).OrderBy(x => x.Cantidad).ThenBy(x => x.Id).Take(6).ToListAsync(ct);
                var cantidad = await stock.CountAsync(x => x.Cantidad <= x.Minimo, ct);
                indicadores.Add(new("stock-bajo", "Productos con stock bajo", cantidad, cantidad, "Productos activos · stock menor o igual al mínimo", "inventario"));
            }
        }
        var resultado = new InicioDto(desde, hoy, depositoId, nombre, DateTime.UtcNow, indicadores, ultimas, bajos, dias, categorias);
        await tx.CommitAsync(ct); return resultado;
    }
    private IQueryable<NexaBack.Infrastructure.Data.Context.Ventas.Entity.Venta> VentasPeriodo(ReporteConsulta q)
    {
        var desde = ReportesReglas.InicioUtc(q.Desde); var hasta = ReportesReglas.InicioUtc(q.Hasta.AddDays(1));
        return context.Ventas.AsNoTracking().Where(x => x.Estado == "Confirmada" && x.CreadoEn >= desde && x.CreadoEn < hasta
            && (!q.DepositoId.HasValue || x.DepositoId == q.DepositoId));
    }
    private async Task<IReadOnlyList<ReporteDiaDto>> Dias(ReporteConsulta q, CancellationToken ct)
    {
        // Agrupar por calendario del negocio, independientemente de la zona horaria de PostgreSQL.
        var filas = await VentasPeriodo(q).GroupBy(x => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.CreadoEn, "America/Argentina/Buenos_Aires").Date)
            .OrderBy(g => g.Key).Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) }).ToListAsync(ct);
        return filas.Select(x => new ReporteDiaDto(DateOnly.FromDateTime(x.Fecha), x.Total)).ToArray();
    }
    private async Task<ReporteVentasDetalleDto> DetalleVentas(ReporteConsulta q, ReporteResumenDto resumen, CancellationToken ct)
    {
        var ids = VentasPeriodo(q).Select(x => x.Id);
        var lineas = context.VentasDetalles.AsNoTracking().Where(x => ids.Contains(x.VentaId));
        var costo = await lineas.SumAsync(x => (decimal?)(x.Cantidad * x.CostoUnitario), ct) ?? 0m;
        var productos = await lineas.GroupBy(x => new { x.ProductoId, x.ProductoCodigo, x.ProductoNombre, x.UnidadMedida })
            .Select(g => new { g.Key, Cantidad = g.Sum(x => x.Cantidad), Bruto = g.Sum(x => x.Subtotal) })
            .OrderByDescending(x => x.Bruto).ThenBy(x => x.Key.ProductoId).Take(5)
            .Select(x => new ReporteProductoDto(x.Key.ProductoId, x.Key.ProductoCodigo, x.Key.ProductoNombre, x.Key.UnidadMedida, x.Cantidad, x.Bruto)).ToListAsync(ct);
        return new(costo, resumen.Importe - costo, resumen.Registros == 0 ? 0 : resumen.Importe / resumen.Registros, await Dias(q, ct), productos);
    }
    private async Task<IReadOnlyList<ReporteCategoriaDto>> Categorias(int? depositoId, CancellationToken ct)
        => await Inventario(depositoId, false).Join(context.Productos.AsNoTracking(), f => f.Id, p => p.Id, (f, p) => new { p.Categoria, f.Importe })
            .GroupBy(x => x.Categoria).OrderBy(g => g.Key)
            .Select(g => new ReporteCategoriaDto(g.Key, g.Count(), g.Sum(x => x.Importe))).ToListAsync(ct);

    private static async Task<ReporteResumenDto> Resumen(IQueryable<ReporteFilaDto> filas, CancellationToken ct)
    {
        var resumen = await filas.GroupBy(x => 1).Select(g => new ReporteResumenDto(g.Count(), g.Sum(x => x.Importe), g.Sum(x => x.Aplicado), g.Sum(x => x.Saldo))).SingleOrDefaultAsync(ct);
        return resumen ?? new(0, 0, 0, 0);
    }
    internal IQueryable<ReporteFilaDto> Filas(ReporteConsulta q)
    {
        var inicio = ReportesReglas.UsaPeriodo(q.Tipo) ? ReportesReglas.InicioUtc(q.Desde) : DateTime.UnixEpoch;
        var fin = ReportesReglas.UsaPeriodo(q.Tipo) ? ReportesReglas.InicioUtc(q.Hasta.AddDays(1)) : DateTime.UnixEpoch;
        var ventas = context.Ventas.AsNoTracking().Where(x => x.Estado == "Confirmada" && (!q.DepositoId.HasValue || x.DepositoId == q.DepositoId));
        var compras = context.Compras.AsNoTracking().Where(x => x.Estado == "Recibida" && (!q.DepositoId.HasValue || x.DepositoId == q.DepositoId));
        switch (q.Tipo)
        {
            case "ventas":
                return ventas.Where(x => x.CreadoEn >= inicio && x.CreadoEn < fin)
                    .Select(x => new { Venta = x, Aplicado = context.Cobros.Where(c => c.VentaId == x.Id && c.Estado == "Vigente").Sum(c => (decimal?)c.Importe) ?? 0m })
                    .Select(x => new ReporteFilaDto { Id = x.Venta.Id, Referencia = "VTA-" + x.Venta.Id.ToString().PadLeft(6, '0'), Fecha = x.Venta.CreadoEn, Nombre = x.Venta.ClienteNombre, Estado = x.Venta.Estado, Importe = x.Venta.Total, Aplicado = x.Aplicado, Saldo = x.Venta.Total - x.Aplicado, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            case "compras":
                return compras.Where(x => x.RecibidoEn >= inicio && x.RecibidoEn < fin)
                    .Select(x => new { Compra = x, Aplicado = context.Pagos.Where(p => p.CompraId == x.Id && p.Estado == "Vigente").Sum(p => (decimal?)p.Importe) ?? 0m })
                    .Select(x => new ReporteFilaDto { Id = x.Compra.Id, Referencia = "OC-" + x.Compra.Id.ToString().PadLeft(6, '0'), Fecha = x.Compra.RecibidoEn, Nombre = x.Compra.ProveedorNombre, Estado = x.Compra.Estado, Importe = x.Compra.Total, Aplicado = x.Aplicado, Saldo = x.Compra.Total - x.Aplicado, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            case "cobros":
                return context.Cobros.AsNoTracking().Where(x => x.Estado == "Vigente" && x.Venta.Estado == "Confirmada" && x.FechaCobro >= q.Desde && x.FechaCobro <= q.Hasta
                    && (!q.DepositoId.HasValue || x.Venta.DepositoId == q.DepositoId))
                    .Select(x => new ReporteFilaDto { Id = x.Id, Referencia = "COB-" + x.Id.ToString().PadLeft(6, '0') + " / VTA-" + x.VentaId.ToString().PadLeft(6, '0'), Fecha = x.FechaCobro.ToDateTime(TimeOnly.MinValue), Nombre = x.Venta.ClienteNombre, Estado = x.MedioPago, Importe = x.Importe, Aplicado = 0, Saldo = 0, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            case "pagos":
                return context.Pagos.AsNoTracking().Where(x => x.Estado == "Vigente" && x.Compra.Estado == "Recibida" && x.FechaPago >= q.Desde && x.FechaPago <= q.Hasta
                    && (!q.DepositoId.HasValue || x.Compra.DepositoId == q.DepositoId))
                    .Select(x => new ReporteFilaDto { Id = x.Id, Referencia = "PAG-" + x.Id.ToString().PadLeft(6, '0') + " / OC-" + x.CompraId.ToString().PadLeft(6, '0'), Fecha = x.FechaPago.ToDateTime(TimeOnly.MinValue), Nombre = x.Compra.ProveedorNombre, Estado = x.MedioPago, Importe = x.Importe, Aplicado = 0, Saldo = 0, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            case "inventario": return Inventario(q.DepositoId, false);
            case "por-cobrar":
                return ventas.Select(x => new { x.ClienteId, x.Total, Aplicado = context.Cobros.Where(c => c.VentaId == x.Id && c.Estado == "Vigente").Sum(c => (decimal?)c.Importe) ?? 0m })
                    .GroupBy(x => x.ClienteId).Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total), Aplicado = g.Sum(x => x.Aplicado) })
                    .Where(x => x.Total > x.Aplicado).Join(context.Clientes.AsNoTracking(), x => x.Id, c => c.Id,
                        (x, c) => new ReporteFilaDto { Id = c.Id, Referencia = c.Documento, Fecha = null, Nombre = c.NombreRazonSocial, Estado = c.Activo ? "Activo" : "Inactivo", Importe = x.Total, Aplicado = x.Aplicado, Saldo = x.Total - x.Aplicado, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            case "por-pagar":
                return compras.Select(x => new { x.ProveedorId, x.Total, Aplicado = context.Pagos.Where(p => p.CompraId == x.Id && p.Estado == "Vigente").Sum(p => (decimal?)p.Importe) ?? 0m })
                    .GroupBy(x => x.ProveedorId).Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total), Aplicado = g.Sum(x => x.Aplicado) })
                    .Where(x => x.Total > x.Aplicado).Join(context.Proveedores.AsNoTracking(), x => x.Id, p => p.Id,
                        (x, p) => new ReporteFilaDto { Id = p.Id, Referencia = p.Documento, Fecha = null, Nombre = p.NombreRazonSocial, Estado = p.Activo ? "Activo" : "Inactivo", Importe = x.Total, Aplicado = x.Aplicado, Saldo = x.Total - x.Aplicado, Cantidad = null, Unidad = null, Costo = null, Minimo = null });
            default: throw new BusinessException("Tipo de reporte inválido.");
        }
    }
    private IQueryable<ReporteFilaDto> Inventario(int? depositoId, bool soloActivos)
        => context.Productos.AsNoTracking().Where(p => !soloActivos || p.Activo)
            .Select(p => new { Producto = p, Cantidad = context.Existencias.Where(e => e.ProductoId == p.Id && (!depositoId.HasValue || e.DepositoId == depositoId)).Sum(e => (decimal?)e.Cantidad) ?? 0m })
            .Select(x => new ReporteFilaDto { Id = x.Producto.Id, Referencia = x.Producto.Codigo, Fecha = null, Nombre = x.Producto.Nombre, Estado = x.Producto.Activo ? "Activo" : "Inactivo", Importe = x.Cantidad * x.Producto.Costo, Aplicado = 0, Saldo = 0, Cantidad = x.Cantidad, Unidad = x.Producto.UnidadMedida, Costo = x.Producto.Costo, Minimo = x.Producto.StockMinimo });
}
