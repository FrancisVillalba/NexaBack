using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Ventas;
namespace NexaBack.Controllers.Ventas;

[ApiController, Authorize, Route("api/ventas")]
public sealed class VentasController(IVentaService service) : ControllerBase
{
    [HttpGet, RequireAccess("ventas.ver")]
    public async Task<ActionResult<VentasPaginaDto>> Listar(int pagina = 1, int tamanoPagina = 20, string? buscar = null, string? estado = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, estado, ct));
    [HttpGet("{id:int}"), RequireAccess("ventas.ver")]
    public async Task<ActionResult<VentaDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));
    [HttpPost("cotizar"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<VentaCotizacionDto>> Cotizar(VentaCotizarRequest request, CancellationToken ct) => Ok(await service.CotizarAsync(request, ct));
    [HttpPost, RequireAccess("ventas.crear")]
    public async Task<ActionResult<VentaDto>> Confirmar(VentaConfirmarRequest request, CancellationToken ct)
    {
        var venta = await service.ConfirmarAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = venta.Id }, venta);
    }
    [HttpPost("{id:int}/anular"), RequireAccess("ventas.anular")]
    public async Task<ActionResult<VentaDto>> Anular(int id, VentaAnularRequest request, CancellationToken ct) => Ok(await service.AnularAsync(id, request, ct));
    [HttpGet("clientes"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<IReadOnlyList<VentaClienteDto>>> Clientes(string buscar, CancellationToken ct) => Ok(await service.ClientesAsync(buscar, ct));
    [HttpGet("productos"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<IReadOnlyList<VentaProductoDto>>> Productos(int depositoId, string buscar, CancellationToken ct) => Ok(await service.ProductosAsync(depositoId, buscar, ct));
    [HttpGet("depositos"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<IReadOnlyList<VentaDepositoDto>>> Depositos(CancellationToken ct) => Ok(await service.DepositosAsync(ct));
}
