using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Compras;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Compras;

namespace NexaBack.Controllers.Compras;

[ApiController, Authorize, Route("api/compras")]
public sealed class ComprasController(ICompraService service, UsuariosDbContext context) : ControllerBase
{
    [HttpGet, RequireAccess("compras.ver")]
    public async Task<ActionResult<ComprasPaginaDto>> Listar(int pagina = 1, int tamanoPagina = 20, string? buscar = null, string? estado = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, estado, ct));
    [HttpGet("{id:int}"), RequireAccess("compras.ver")]
    public async Task<ActionResult<CompraDto>> Obtener(int id, CancellationToken ct) => Ok(await context.ConNombresAsync(await service.ObtenerAsync(id, ct), ct));
    [HttpPost, RequireAccess("compras.crear")]
    public async Task<ActionResult<CompraDto>> Crear(CrearCompraRequest request, CancellationToken ct)
    {
        var compra = await context.ConNombresAsync(await service.CrearAsync(request, ct), ct);
        return CreatedAtAction(nameof(Obtener), new { id = compra.Id }, compra);
    }
    [HttpPut("{id:int}"), RequireAccess("compras.crear")]
    public async Task<ActionResult<CompraDto>> Editar(int id, EditarCompraRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.EditarAsync(id, request, ct), ct));
    [HttpPost("{id:int}/recibir"), RequireAccess("compras.recibir")]
    public async Task<ActionResult<CompraDto>> Recibir(int id, CompraEstadoRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.RecibirAsync(id, request, ct), ct));
    [HttpPost("{id:int}/cancelar"), RequireAccess("compras.cancelar")]
    public async Task<ActionResult<CompraDto>> Cancelar(int id, CompraEstadoRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.CancelarAsync(id, request, ct), ct));
    [HttpGet("proveedores"), RequireAccess("compras.crear")]
    public async Task<ActionResult<IReadOnlyList<CompraProveedorDto>>> Proveedores(string buscar, CancellationToken ct)
        => Ok(await service.ProveedoresAsync(buscar, ct));
    [HttpGet("productos"), RequireAccess("compras.crear")]
    public async Task<ActionResult<IReadOnlyList<CompraProductoDto>>> Productos(string buscar, CancellationToken ct)
        => Ok(await service.ProductosAsync(buscar, ct));
    [HttpGet("depositos"), RequireAccess("compras.crear")]
    public async Task<ActionResult<IReadOnlyList<CompraDepositoDto>>> Depositos(CancellationToken ct)
        => Ok(await service.DepositosAsync(ct));
}
