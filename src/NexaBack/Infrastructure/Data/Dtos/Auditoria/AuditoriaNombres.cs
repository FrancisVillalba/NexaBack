namespace NexaBack.Infrastructure.Data.Dtos.Auditoria;

public sealed record AuditoriaNombres(string? CreadoPor, string? ModificadoPor, string? AprobadoPor);

public interface IAuditoriaVisible
{
    int? CreadoPor { get; }
    int? ModificadoPor => null;
    int? AprobadoPor => null;
    AuditoriaNombres? NombresAuditoria { get; set; }
}
