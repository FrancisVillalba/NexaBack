using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Presupuestos;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Presupuestos;
using NexaBack.Services.Ventas;
namespace NexaBack.Controllers.Presupuestos;

[ApiController, Authorize, Route("api/presupuestos")]
public sealed class PresupuestosController(IPresupuestoService service, IVentaService catalogos, UsuariosDbContext context) : ControllerBase
{
    [HttpGet, RequireAccess("presupuestos.ver")]
    public async Task<ActionResult<PresupuestosPaginaDto>> Listar(int pagina = 1, int tamanoPagina = 20, string? buscar = null, string? estado = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, estado, ct));
    [HttpGet("{id:int}"), RequireAccess("presupuestos.ver")]
    public async Task<ActionResult<PresupuestoDto>> Obtener(int id, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.ObtenerAsync(id, ct), ct));
    [HttpPost("cotizar"), Authorize(Policy = "presupuestos.preparar")]
    public async Task<ActionResult<VentaCotizacionDto>> Cotizar(VentaCotizarRequest request, CancellationToken ct) => Ok(await service.CotizarAsync(request, ct));
    [HttpPost, RequireAccess("presupuestos.crear")]
    public async Task<ActionResult<PresupuestoDto>> Crear(PresupuestoGuardarRequest request, CancellationToken ct)
    {
        var p = await context.ConNombresAsync(await service.GuardarAsync(null, request, ct), ct);
        return CreatedAtAction(nameof(Obtener), new { id = p.Id }, p);
    }
    [HttpPut("{id:int}"), RequireAccess("presupuestos.editar")]
    public async Task<ActionResult<PresupuestoDto>> Editar(int id, PresupuestoGuardarRequest request, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.GuardarAsync(id, request, ct), ct));
    [HttpPost("{id:int}/aprobar"), RequireAccess("presupuestos.aprobar")]
    public async Task<ActionResult<PresupuestoDto>> Aprobar(int id, PresupuestoVersionRequest request, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.AprobarAsync(id, request.Version, ct), ct));
    [HttpPost("{id:int}/cancelar"), RequireAccess("presupuestos.cancelar")]
    public async Task<ActionResult<PresupuestoDto>> Cancelar(int id, PresupuestoCancelarRequest request, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.CancelarAsync(id, request, ct), ct));
    [HttpPost("{id:int}/preparar-conversion"), RequireAccess("presupuestos.convertir"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<PresupuestoConversionDto>> Preparar(int id, CancellationToken ct) => Ok(await service.PrepararConversionAsync(id, ct));
    [HttpPost("{id:int}/convertir"), RequireAccess("presupuestos.convertir"), RequireAccess("ventas.crear")]
    public async Task<ActionResult<VentaDto>> Convertir(int id, PresupuestoConvertirRequest request, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.ConvertirAsync(id, request, ct), ct));
    [HttpGet("clientes"), Authorize(Policy = "presupuestos.preparar")]
    public async Task<ActionResult<IReadOnlyList<VentaClienteDto>>> Clientes(string buscar, CancellationToken ct) => Ok(await catalogos.ClientesAsync(buscar, ct));
    [HttpGet("productos"), Authorize(Policy = "presupuestos.preparar")]
    public async Task<ActionResult<IReadOnlyList<VentaProductoDto>>> Productos(int depositoId, string buscar, CancellationToken ct) => Ok(await catalogos.ProductosAsync(depositoId, buscar, ct));
    [HttpGet("depositos"), Authorize(Policy = "presupuestos.preparar")]
    public async Task<ActionResult<IReadOnlyList<VentaDepositoDto>>> Depositos(CancellationToken ct) => Ok(await catalogos.DepositosAsync(ct));
}
