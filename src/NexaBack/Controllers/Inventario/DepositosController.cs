using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Inventario;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Inventario;

namespace NexaBack.Controllers.Inventario;

[ApiController, Authorize(Roles = "Administrador")]
[Route("api/depositos")]
public sealed class DepositosController(IDepositoService service) : ControllerBase
{
    [HttpGet, RequireAccess("depositos.ver")]
    public async Task<ActionResult<DepositosPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, ct));

    [HttpGet("{id:int}"), RequireAccess("depositos.ver")]
    public async Task<ActionResult<DepositoAdministracionDto>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost, RequireAccess("depositos.crear")]
    public async Task<ActionResult<DepositoAdministracionDto>> Crear(GuardarDepositoRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}"), RequireAccess("depositos.editar")]
    public async Task<ActionResult<DepositoAdministracionDto>> Actualizar(int id, GuardarDepositoRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado"), RequireAccess("depositos.estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoDepositoRequest request, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, request, ct);
        return NoContent();
    }
}
