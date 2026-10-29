using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Usuarios;
using NexaBack.Services.Usuarios.Usuarios;

namespace NexaBack.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios/usuarios")]
[Authorize(Roles = "Administrador")]
public sealed class UsuariosController(IUsuarioService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear(
        [FromBody] CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }
}
