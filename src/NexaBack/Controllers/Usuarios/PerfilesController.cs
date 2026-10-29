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
    [ProducesResponseType<IReadOnlyList<PerfilDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PerfilDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerfilDto>> Crear(
        [FromBody] CrearPerfilRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }
}
