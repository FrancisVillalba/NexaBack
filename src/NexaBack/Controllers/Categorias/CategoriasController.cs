using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Categorias;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Categorias;

namespace NexaBack.Controllers.Categorias;

[ApiController, Authorize(Roles = "Administrador")]
[Route("api/categorias")]
public sealed class CategoriasController(ICategoriaService service) : ControllerBase
{
    [HttpGet, RequireAccess("categorias.ver")]
    public async Task<ActionResult<CategoriasPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, ct));

    [HttpGet("{id:int}"), RequireAccess("categorias.ver")]
    public async Task<ActionResult<CategoriaAdministracionDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost, RequireAccess("categorias.crear")]
    public async Task<ActionResult<CategoriaAdministracionDto>> Crear(GuardarCategoriaRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}"), RequireAccess("categorias.editar")]
    public async Task<ActionResult<CategoriaAdministracionDto>> Actualizar(int id, GuardarCategoriaRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado"), RequireAccess("categorias.estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoCategoriaRequest request, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, request, ct);
        return NoContent();
    }
}
