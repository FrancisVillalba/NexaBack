using NexaBack.Infrastructure.Data.Dtos.Reportes;
using NexaBack.Repository.Reportes;
namespace NexaBack.Services.Reportes;

public sealed class ReportesService(IReportesRepository repository) : IReportesService
{
    public Task<IReadOnlyList<ReporteDepositoDto>> DepositosAsync(CancellationToken ct) => repository.DepositosAsync(ct);
    public Task<ReporteDto> ConsultarAsync(ReporteConsulta consulta, bool exportar, IReadOnlySet<string> permisos, CancellationToken ct)
    {
        ReportesReglas.Validar(consulta);
        ReportesReglas.Autorizar(consulta.Tipo, permisos);
        return repository.ConsultarAsync(consulta, exportar, ct);
    }
    public Task<InicioDto> InicioAsync(int? depositoId, IReadOnlySet<string> permisos, CancellationToken ct)
    {
        ReportesReglas.Deposito(depositoId);
        return repository.InicioAsync(depositoId, permisos, ct);
    }
}
