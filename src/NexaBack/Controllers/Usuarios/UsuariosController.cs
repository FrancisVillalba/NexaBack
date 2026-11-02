using NexaBack.Infrastructure.Security;
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
    [RequireAccess("usuarios.ver")]
    [ProducesResponseType<IReadOnlyList<UsuarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.ListarAsync(pagina, tamanoPagina, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [RequireAccess("usuarios.ver")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        return Ok(await service.ObtenerAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequireAccess("usuarios.crear")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear(
        [FromBody] CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [RequireAccess("usuarios.editar")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Actualizar(int id,
        [FromBody] ActualizarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await service.ActualizarAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/estado")]
    [RequireAccess("usuarios.estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(int id,
        [FromBody] CambiarEstadoUsuarioRequest request, CancellationToken cancellationToken)
    {
        await service.CambiarEstadoAsync(id, request.Activo, cancellationToken);
        return NoContent();
    }

    // Solo Administrador puede establecer una contraseña nueva. Nunca se devuelve su hash.
    [HttpPatch("{id:int}/password")]
    [RequireAccess("usuarios.password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarPassword(int id,
        [FromBody] CambiarPasswordUsuarioRequest request, CancellationToken cancellationToken)
    {
        await service.CambiarPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
