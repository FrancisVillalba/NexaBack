using NexaBack.Infrastructure.Data.Dtos.Reportes;
namespace NexaBack.Services.Reportes;

public interface IReportesService
{
    Task<IReadOnlyList<ReporteDepositoDto>> DepositosAsync(CancellationToken ct);
    Task<ReporteDto> ConsultarAsync(ReporteConsulta consulta, bool exportar, IReadOnlySet<string> permisos, CancellationToken ct);
    Task<InicioDto> InicioAsync(int? depositoId, IReadOnlySet<string> permisos, CancellationToken ct);
}
