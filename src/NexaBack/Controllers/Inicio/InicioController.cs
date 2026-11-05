using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Reportes;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Reportes;
namespace NexaBack.Controllers.Inicio;

[ApiController, Authorize, Route("api/inicio"), RequireAccess("inicio.ver")]
public sealed class InicioController(IReportesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<InicioDto>> Obtener(int? depositoId, CancellationToken ct)
        => Ok(await service.InicioAsync(depositoId, User.FindAll("acceso").Select(x => x.Value).ToHashSet(StringComparer.Ordinal), ct));
    [HttpGet("depositos")]
    public async Task<ActionResult<IReadOnlyList<ReporteDepositoDto>>> Depositos(CancellationToken ct) => Ok(await service.DepositosAsync(ct));
}
