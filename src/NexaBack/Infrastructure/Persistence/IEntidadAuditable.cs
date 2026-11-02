namespace NexaBack.Infrastructure.Persistence;

// Toda entidad persistida debe implementar este contrato de auditoría.
// Los identificadores nulos representan datos históricos o procesos sin usuario autenticado.
public interface IEntidadAuditable
{
    DateTime CreadoEn { get; set; }
    int? CreadoPor { get; set; }
    DateTime? ModificadoEn { get; set; }
    int? ModificadoPor { get; set; }
}
