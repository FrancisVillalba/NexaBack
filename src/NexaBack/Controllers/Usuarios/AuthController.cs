using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexaBack.Infrastructure.Data.Dtos.Usuarios;
using NexaBack.Services.Usuarios.Usuarios;

namespace NexaBack.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios/auth")]
public sealed class AuthController(IUsuarioService usuarios) : ControllerBase
{
    // Permite verificar una política concreta sin emitir otro token ni exponer datos ajenos.
    [HttpGet("accesos/{codigo}")]
    [Authorize]
    public async Task<IActionResult> ComprobarAcceso(string codigo, [FromServices] IAuthorizationService authorization)
    {
        if (!NexaBack.Infrastructure.Security.AccessCatalog.Definitions.Any(x => x.Codigo == codigo)) return NotFound();
        var result = await authorization.AuthorizeAsync(User, "acceso:" + codigo);
        return result.Succeeded ? NoContent() : Forbid();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioSesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioSesionDto>> Me(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id) || id <= 0) return Unauthorized();
        var usuario = await usuarios.ObtenerSesionAsync(id, cancellationToken);
        // La sesión devuelve los mismos permisos del token que validó el middleware.
        return Ok(usuario with { Accesos = User.FindAll("acceso").Select(x => x.Value).Distinct().ToArray() });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(await usuarios.LoginAsync(request, cancellationToken));
    }
}
