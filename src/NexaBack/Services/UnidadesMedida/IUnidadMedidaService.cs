using NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;
using NexaBack.Infrastructure.Data.Dtos.UnidadesMedida;
namespace NexaBack.Services.UnidadesMedida;
public interface IUnidadMedidaService
{
    Task<UnidadesMedidaOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct);
    Task<UnidadMedida> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct);
}
