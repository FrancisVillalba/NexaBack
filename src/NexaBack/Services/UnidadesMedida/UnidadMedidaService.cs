using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;
using NexaBack.Infrastructure.Data.Dtos.UnidadesMedida;
using NexaBack.Repository.UnidadesMedida;
namespace NexaBack.Services.UnidadesMedida;
public sealed class UnidadMedidaService(IUnidadMedidaRepository repository) : IUnidadMedidaService
{
    public async Task<UnidadesMedidaOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var (items, total) = await repository.ListarAsync(offset, size, ct);
        return new(items.Select(x => new UnidadMedidaOpcionDto(x.Id, x.Nombre, x.Activo, x.Codigo, x.Codigo is "metro" or "kilo")).ToArray(), total, pagina, size);
    }
    public async Task<UnidadMedida> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct)
    {
        if (id <= 0) throw new BusinessException("Seleccioná una unidad de medida.");
        var seleccion = await repository.ObtenerAsync(id, ct) ?? throw new BusinessException("La unidad de medida no existe.");
        // Una referencia inactiva solo se puede conservar en el producto que ya la tenía.
        if (!seleccion.Activo && id != actualId) throw new BusinessException("La unidad de medida está inactiva.");
        return seleccion;
    }
}
