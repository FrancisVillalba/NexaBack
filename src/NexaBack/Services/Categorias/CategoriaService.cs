using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Dtos.Categorias;
using NexaBack.Repository.Categorias;
namespace NexaBack.Services.Categorias;
public sealed class CategoriaService(ICategoriaRepository repository) : ICategoriaService
{
    public async Task<CategoriasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var (items, total) = await repository.ListarAsync(offset, size, ct);
        return new(items.Select(x => new CategoriaOpcionDto(x.Id, x.Nombre, x.Activo)).ToArray(), total, pagina, size);
    }
    public async Task<Categoria> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct)
    {
        if (id <= 0) throw new BusinessException("Seleccioná una categoría.");
        var seleccion = await repository.ObtenerAsync(id, ct) ?? throw new BusinessException("La categoría no existe.");
        // Una referencia inactiva solo se puede conservar en el producto que ya la tenía.
        if (!seleccion.Activo && id != actualId) throw new BusinessException("La categoría está inactiva.");
        return seleccion;
    }
}
