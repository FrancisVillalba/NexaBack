using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Pagos;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Pagos;
namespace NexaBack.Controllers.Pagos;

[ApiController, Authorize, Route("api/compras/{compraId:int}/pagos")]
public sealed class PagosController(IPagoService service, UsuariosDbContext context) : ControllerBase
{
    [HttpGet, RequireAccess("compras.ver")]
    public async Task<ActionResult<CompraPagosDto>> Listar(int compraId, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        var resultado = await service.ListarAsync(compraId, pagina, tamanoPagina, ct);
        await context.CompletarNombresAsync(resultado.Items, ct);
        return Ok(resultado);
    }
    [HttpPost, RequireAccess("compras.pagar")]
    public async Task<ActionResult<PagoDto>> Registrar(int compraId, RegistrarPagoRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.RegistrarAsync(compraId, request, ct), ct));
    [HttpPost("{pagoId:int}/anular"), RequireAccess("compras.anular_pago")]
    public async Task<ActionResult<PagoDto>> Anular(int compraId, int pagoId, AnularPagoRequest request, CancellationToken ct)
        => Ok(await context.ConNombresAsync(await service.AnularAsync(compraId, pagoId, request, ct), ct));
}
