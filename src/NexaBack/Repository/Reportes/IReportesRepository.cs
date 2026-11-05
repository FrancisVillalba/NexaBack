using NexaBack.Infrastructure.Data.Dtos.Reportes;
namespace NexaBack.Repository.Reportes;

public interface IReportesRepository
{
    Task<IReadOnlyList<ReporteDepositoDto>> DepositosAsync(CancellationToken ct);
    Task<ReporteDto> ConsultarAsync(ReporteConsulta consulta, bool exportar, CancellationToken ct);
    Task<InicioDto> InicioAsync(int? depositoId, IReadOnlySet<string> permisos, CancellationToken ct);
}
