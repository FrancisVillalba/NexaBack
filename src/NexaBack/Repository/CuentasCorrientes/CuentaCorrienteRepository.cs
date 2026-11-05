using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
using NexaBack.Repository.Cobros;
using NexaBack.Services.Cobros;
namespace NexaBack.Repository.CuentasCorrientes;

public sealed class CuentaCorrienteRepository(UsuariosDbContext context) : ICuentaCorrienteRepository
{
    public async Task<CuentaCorrienteDto> ObtenerAsync(int clienteId, int paginaVentas, int paginaCobros, int tamanoPagina, CancellationToken ct)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var cliente = await context.Clientes.AsNoTracking().Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == clienteId, ct) ?? throw new BusinessException("Cliente no encontrado.", 404);
        var ventas = context.Ventas.AsNoTracking().Where(x => x.ClienteId == clienteId);
        var cobros = context.Cobros.AsNoTracking().Where(x => x.Venta.ClienteId == clienteId);
        var totalVentas = await ventas.Where(x => x.Estado == "Confirmada").SumAsync(x => (decimal?)x.Total, ct) ?? 0m;
        var totalCobros = await cobros.Where(x => x.Estado == "Vigente" && x.Venta.Estado == "Confirmada").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
        var cantidadVentas = await ventas.CountAsync(ct);
        var cantidadCobros = await cobros.CountAsync(ct);
        var filas = await ventas.OrderByDescending(x => x.Id).Skip((paginaVentas - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(v => new { Venta = v, Cobrado = context.Cobros.Where(c => c.VentaId == v.Id && c.Estado == "Vigente").Sum(c => (decimal?)c.Importe) ?? 0m }).ToListAsync(ct);
        var filasCobros = await cobros.OrderByDescending(x => x.Id).Skip((paginaCobros - 1) * tamanoPagina).Take(tamanoPagina).Select(CobroProyeccion.Dto).ToListAsync(ct);
        var resultado = new CuentaCorrienteDto(cliente.Id, cliente.NombreRazonSocial, cliente.TipoDocumento == null ? null : cliente.TipoDocumento.Nombre, cliente.Documento, cliente.Activo,
            totalVentas, totalCobros, totalVentas - totalCobros,
            filas.Select(x => new CuentaVentaDto(x.Venta.Id, "VTA-" + x.Venta.Id.ToString("D6"), x.Venta.CreadoEn, x.Venta.Estado,
                x.Venta.Total, x.Cobrado, CobroReglas.Saldo(x.Venta.Estado, x.Venta.Total, x.Cobrado),
                CobroReglas.Estado(x.Venta.Estado, x.Venta.Total, x.Cobrado))).ToArray(), cantidadVentas, paginaVentas,
            filasCobros, cantidadCobros, paginaCobros, tamanoPagina);
        await transaccion.CommitAsync(ct);
        return resultado;
    }
}
