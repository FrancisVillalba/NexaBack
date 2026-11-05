using System.Data;
using Microsoft.EntityFrameworkCore;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Usuarios;
using NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
using NexaBack.Repository.Pagos;
using NexaBack.Services.Pagos;
namespace NexaBack.Repository.CuentasProveedores;

public sealed class CuentaProveedorRepository(UsuariosDbContext context) : ICuentaProveedorRepository
{
    public async Task<CuentaProveedorDto> ObtenerAsync(int proveedorId, int paginaCompras, int paginaPagos, int tamanoPagina, CancellationToken ct)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var proveedor = await context.Proveedores.AsNoTracking().Include(x => x.TipoDocumento).SingleOrDefaultAsync(x => x.Id == proveedorId, ct) ?? throw new BusinessException("Proveedor no encontrado.", 404);
        var compras = context.Compras.AsNoTracking().Where(x => x.ProveedorId == proveedorId);
        var pagos = context.Pagos.AsNoTracking().Where(x => x.Compra.ProveedorId == proveedorId);
        var totalCompras = await compras.Where(x => x.Estado == "Recibida").SumAsync(x => (decimal?)x.Total, ct) ?? 0m;
        var totalPagos = await pagos.Where(x => x.Estado == "Vigente" && x.Compra.Estado == "Recibida").SumAsync(x => (decimal?)x.Importe, ct) ?? 0m;
        var cantidadCompras = await compras.CountAsync(ct);
        var cantidadPagos = await pagos.CountAsync(ct);
        var filas = await compras.OrderByDescending(x => x.Id).Skip((paginaCompras - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(v => new { Compra = v, Pagado = context.Pagos.Where(c => c.CompraId == v.Id && c.Estado == "Vigente").Sum(c => (decimal?)c.Importe) ?? 0m }).ToListAsync(ct);
        var filasPagos = await pagos.OrderByDescending(x => x.Id).Skip((paginaPagos - 1) * tamanoPagina).Take(tamanoPagina).Select(PagoProyeccion.Dto).ToListAsync(ct);
        var resultado = new CuentaProveedorDto(proveedor.Id, proveedor.NombreRazonSocial, proveedor.TipoDocumento == null ? null : proveedor.TipoDocumento.Nombre, proveedor.Documento, proveedor.Activo,
            totalCompras, totalPagos, totalCompras - totalPagos,
            filas.Select(x => new CuentaCompraDto(x.Compra.Id, "OC-" + x.Compra.Id.ToString("D6"), x.Compra.CreadoEn, x.Compra.Estado,
                x.Compra.Total, x.Pagado, PagoReglas.Saldo(x.Compra.Estado, x.Compra.Total, x.Pagado),
                PagoReglas.Estado(x.Compra.Estado, x.Compra.Total, x.Pagado))).ToArray(), cantidadCompras, paginaCompras,
            filasPagos, cantidadPagos, paginaPagos, tamanoPagina);
        await transaccion.CommitAsync(ct);
        return resultado;
    }
}
