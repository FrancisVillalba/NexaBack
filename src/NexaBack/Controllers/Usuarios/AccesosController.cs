using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Usuarios.Perfiles;

namespace NexaBack.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios/accesos")]
[Authorize(Roles = "Administrador")]
public sealed class AccesosController(IAccesoService service) : ControllerBase
{
    [HttpGet, RequireAccess("permisos.ver")]
    public async Task<ActionResult<AccesoDto[]>> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    [HttpGet("perfiles/{id:int}"), RequireAccess("permisos.ver")]
    public async Task<ActionResult<PerfilAccesosDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPut("perfiles/{id:int}"), RequireAccess("permisos.editar")]
    public async Task<ActionResult<PerfilAccesosDto>> Guardar(int id, GuardarAccesosRequest request, CancellationToken ct)
        => Ok(await service.GuardarAsync(id, request, ct));
}
