using NexaBack.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Personas;
using NexaBack.Services.Usuarios.Personas;

namespace NexaBack.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios/personas")]
[Authorize(Roles = "Administrador")]
public sealed class PersonasController(IPersonaService service) : ControllerBase
{
    [HttpGet]
    [RequireAccess("personas.ver")]
    [ProducesResponseType<IReadOnlyList<PersonaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonaDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [RequireAccess("personas.ver")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequireAccess("personas.crear")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Crear(
        [FromBody] CrearPersonaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [RequireAccess("personas.editar")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Actualizar(int id,
        [FromBody] ActualizarPersonaRequest request, CancellationToken cancellationToken)
        => Ok(await service.ActualizarAsync(id, request, cancellationToken));

    // La baja es lógica: conservamos los datos y las relaciones del usuario.
    [HttpPatch("{id:int}/estado")]
    [RequireAccess("personas.estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(int id,
        [FromBody] CambiarEstadoPersonaRequest request, CancellationToken cancellationToken)
    {
        await service.CambiarEstadoAsync(id, request.Activo, cancellationToken);
        return NoContent();
    }
}
