using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.CuentasCorrientes;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.CuentasCorrientes;
namespace NexaBack.Controllers.CuentasCorrientes;

[ApiController, Authorize, Route("api/clientes/{clienteId:int}/cuenta-corriente")]
public sealed class CuentasCorrientesController(ICuentaCorrienteService service) : ControllerBase
{
    [HttpGet, RequireAccess("clientes.cuenta_corriente")]
    public async Task<ActionResult<CuentaCorrienteDto>> Obtener(int clienteId, int paginaVentas = 1, int paginaCobros = 1, int tamanoPagina = 20, CancellationToken ct = default)
        => Ok(await service.ObtenerAsync(clienteId, paginaVentas, paginaCobros, tamanoPagina, ct));
}
