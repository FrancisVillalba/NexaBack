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
    [ProducesResponseType<IReadOnlyList<PersonaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonaDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Crear(
        [FromBody] CrearPersonaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }
}
