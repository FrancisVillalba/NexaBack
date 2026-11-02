using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Productos;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Productos;

namespace NexaBack.Controllers.Productos;

[ApiController]
[Authorize]
[Route("api/productos")]
public sealed class ProductosController(IProductoService service) : ControllerBase
{
    [HttpGet, RequireAccess("productos.ver")]
    public async Task<ActionResult<ProductosPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, ct));

    [HttpGet("{id:int}"), RequireAccess("productos.ver")]
    public async Task<ActionResult<ProductoDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost, RequireAccess("productos.crear")]
    public async Task<ActionResult<ProductoDto>> Crear(GuardarProductoRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}"), RequireAccess("productos.editar")]
    public async Task<ActionResult<ProductoDto>> Actualizar(int id, GuardarProductoRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado"), RequireAccess("productos.estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoProductoRequest request, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, request, ct);
        return NoContent();
    }
}
