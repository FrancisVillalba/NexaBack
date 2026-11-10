using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Inventario;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Inventario;

namespace NexaBack.Controllers.Inventario;

[ApiController, Authorize]
[Route("api/inventario")]
public sealed class InventarioController(IInventarioService service, UsuariosDbContext context) : ControllerBase
{
    // Tanto consulta como registro requieren conocer los depósitos disponibles.
    [HttpGet("depositos")]
    public async Task<ActionResult<IReadOnlyList<DepositoDto>>> Depositos(CancellationToken ct)
    {
        if (!User.HasClaim("acceso", "inventario.ver") && !User.HasClaim("acceso", "movimientos.ver") && !User.HasClaim("acceso", "movimientos.crear")) return Forbid();
        return Ok(await service.DepositosAsync(ct));
    }

    [HttpGet, RequireAccess("inventario.ver")]
    public async Task<ActionResult<InventarioPaginaDto>> Listar([FromQuery] int depositoId, [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20, [FromQuery] string? buscar = null, [FromQuery] string estado = "todos",
        [FromQuery] bool? activo = null, CancellationToken ct = default)
        => Ok(await service.ListarAsync(depositoId, pagina, tamanoPagina, buscar, estado, activo, ct));

    [HttpGet("movimientos"), RequireAccess("movimientos.ver")]
    public async Task<ActionResult<MovimientosPaginaDto>> Movimientos([FromQuery] int? depositoId = null, [FromQuery] int? productoId = null,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20, [FromQuery] string? buscar = null, CancellationToken ct = default)
    {
        var resultado = await service.MovimientosAsync(depositoId, productoId, pagina, tamanoPagina, buscar, ct);
        await context.CompletarNombresAsync(resultado.Items, ct);
        return Ok(resultado);
    }

    [HttpPost("movimientos"), RequireAccess("movimientos.crear")]
    public async Task<ActionResult<MovimientoDto>> Registrar(RegistrarMovimientoRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.RegistrarAsync(request, ct), ct));
}
