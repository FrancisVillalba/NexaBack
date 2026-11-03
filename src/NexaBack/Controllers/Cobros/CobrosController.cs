using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Cobros;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Cobros;
namespace NexaBack.Controllers.Cobros;

[ApiController, Authorize, Route("api/ventas/{ventaId:int}/cobros")]
public sealed class CobrosController(ICobroService service) : ControllerBase
{
    [HttpGet, RequireAccess("ventas.ver")]
    public async Task<ActionResult<VentaCobrosDto>> Listar(int ventaId, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
        => Ok(await service.ListarAsync(ventaId, pagina, tamanoPagina, ct));
    [HttpPost, RequireAccess("ventas.cobrar")]
    public async Task<ActionResult<CobroDto>> Registrar(int ventaId, RegistrarCobroRequest request, CancellationToken ct)
        => Ok(await service.RegistrarAsync(ventaId, request, ct));
    [HttpPost("{cobroId:int}/anular"), RequireAccess("ventas.anular_cobro")]
    public async Task<ActionResult<CobroDto>> Anular(int ventaId, int cobroId, AnularCobroRequest request, CancellationToken ct)
        => Ok(await service.AnularAsync(ventaId, cobroId, request, ct));
}
