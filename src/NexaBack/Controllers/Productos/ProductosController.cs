using NexaBack.Services.Categorias;
using NexaBack.Services.UnidadesMedida;
using NexaBack.Infrastructure.Data.Dtos.Categorias;
using NexaBack.Infrastructure.Data.Dtos.UnidadesMedida;
using NexaBack.Services.Marcas;
using NexaBack.Infrastructure.Data.Dtos.Marcas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Productos;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Productos;

namespace NexaBack.Controllers.Productos;

[ApiController]
[Authorize]
[Route("api/productos")]
public sealed class ProductosController(IProductoService service, IMarcaService marcas, ICategoriaService categorias, IUnidadMedidaService unidades) : ControllerBase
{
    // Catálogo mínimo para el selector: no habilita el ABM administrativo.
    [HttpGet("marcas"), Authorize(Policy = "productos.marcas")]
    public async Task<ActionResult<MarcasOpcionesPaginaDto>> Marcas(int pagina = 1, int tamanoPagina = 100, CancellationToken ct = default)
        => Ok(await marcas.OpcionesAsync(pagina, tamanoPagina, ct));

    [HttpGet("categorias"), Authorize(Policy = "productos.catalogos")]
    public async Task<ActionResult<CategoriasOpcionesPaginaDto>> Categorias(int pagina = 1, int tamanoPagina = 100, CancellationToken ct = default)
        => Ok(await categorias.OpcionesAsync(pagina, tamanoPagina, ct));

    [HttpGet("unidades-medida"), Authorize(Policy = "productos.catalogos")]
    public async Task<ActionResult<UnidadesMedidaOpcionesPaginaDto>> Unidades(int pagina = 1, int tamanoPagina = 100, CancellationToken ct = default)
        => Ok(await unidades.OpcionesAsync(pagina, tamanoPagina, ct));

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
