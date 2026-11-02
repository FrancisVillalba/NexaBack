using NexaBack.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;
using NexaBack.Services.Usuarios.Perfiles;

namespace NexaBack.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios/perfiles")]
[Authorize(Roles = "Administrador")]
public sealed class PerfilesController(IPerfilService service) : ControllerBase
{
    [HttpGet]
    [RequireAccess("perfiles.ver")]
    [ProducesResponseType<IReadOnlyList<PerfilDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PerfilDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [RequireAccess("perfiles.ver")]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequireAccess("perfiles.crear")]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerfilDto>> Crear(
        [FromBody] CrearPerfilRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [RequireAccess("perfiles.editar")]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerfilDto>> Actualizar(int id,
        [FromBody] ActualizarPerfilRequest request, CancellationToken cancellationToken)
        => Ok(await service.ActualizarAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/estado")]
    [RequireAccess("perfiles.estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(int id,
        [FromBody] CambiarEstadoPerfilRequest request, CancellationToken cancellationToken)
    {
        await service.CambiarEstadoAsync(id, request.Activo, cancellationToken);
        return NoContent();
    }
}
