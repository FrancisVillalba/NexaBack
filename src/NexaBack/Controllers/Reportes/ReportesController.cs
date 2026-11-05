using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.Reportes;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.Reportes;
namespace NexaBack.Controllers.Reportes;

[ApiController, Authorize, Route("api/reportes"), RequireAccess("reportes.ver")]
public sealed class ReportesController(IReportesService service) : ControllerBase
{
    private HashSet<string> Permisos() => User.FindAll("acceso").Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
    [HttpGet]
    public async Task<ActionResult<ReporteDto>> Consultar([FromQuery] ReporteConsulta consulta, CancellationToken ct)
        => Ok(await service.ConsultarAsync(consulta, false, Permisos(), ct));
    [HttpGet("exportar"), RequireAccess("reportes.exportar")]
    public async Task<ActionResult<ReporteDto>> Exportar([FromQuery] ReporteConsulta consulta, CancellationToken ct)
        => Ok(await service.ConsultarAsync(consulta, true, Permisos(), ct));
    [HttpGet("depositos")]
    public async Task<ActionResult<IReadOnlyList<ReporteDepositoDto>>> Depositos(CancellationToken ct) => Ok(await service.DepositosAsync(ct));
}
