using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Dtos.Categorias;
namespace NexaBack.Services.Categorias;
public interface ICategoriaService
{
    Task<CategoriasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct);
    Task<Categoria> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct);
}
