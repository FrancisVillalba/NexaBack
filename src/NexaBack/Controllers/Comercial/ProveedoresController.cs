using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Comercial;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Proveedores;

namespace NexaBack.Controllers.Comercial;

[ApiController, Authorize]
[Route("api/proveedores")]
public sealed class ProveedoresController(IProveedorService service) : ControllerBase
{
    [HttpGet, RequireAccess("proveedores.ver")]
    public async Task<ActionResult<ContactosPaginaDto>> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? buscar = null, [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(pagina, tamanoPagina, buscar, activo, ct));

    [HttpGet("{id:int}"), RequireAccess("proveedores.ver")]
    public async Task<ActionResult<ContactoComercialDto>> Obtener(int id, CancellationToken ct)
        => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost, RequireAccess("proveedores.crear")]
    public async Task<ActionResult<ContactoComercialDto>> Crear(GuardarContactoRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}"), RequireAccess("proveedores.editar")]
    public async Task<ActionResult<ContactoComercialDto>> Actualizar(int id, GuardarContactoRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado"), RequireAccess("proveedores.estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoContactoRequest request, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, request, ct);
        return NoContent();
    }
}
