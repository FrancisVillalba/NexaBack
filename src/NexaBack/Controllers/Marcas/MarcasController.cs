using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Marcas;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Marcas;

namespace NexaBack.Controllers.Marcas;

[ApiController, Authorize(Roles = "Administrador")]
[Route("api/marcas")]
public sealed class MarcasController(IMarcaService service) : ControllerBase
{
    [HttpGet, RequireAccess("marcas.ver")]
    public async Task<ActionResult<MarcasPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, ct));

    [HttpGet("{id:int}"), RequireAccess("marcas.ver")]
    public async Task<ActionResult<MarcaAdministracionDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost, RequireAccess("marcas.crear")]
    public async Task<ActionResult<MarcaAdministracionDto>> Crear(GuardarMarcaRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}"), RequireAccess("marcas.editar")]
    public async Task<ActionResult<MarcaAdministracionDto>> Actualizar(int id, GuardarMarcaRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado"), RequireAccess("marcas.estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoMarcaRequest request, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, request, ct);
        return NoContent();
    }
}
