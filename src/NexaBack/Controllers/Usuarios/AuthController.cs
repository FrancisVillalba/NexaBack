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
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioSesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioSesionDto>> Me(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id) || id <= 0) return Unauthorized();
        return Ok(await usuarios.ObtenerSesionAsync(id, cancellationToken));
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
