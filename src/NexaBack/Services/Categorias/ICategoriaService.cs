using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Dtos.Categorias;

namespace NexaBack.Services.Categorias;

public interface ICategoriaService
{
    Task<CategoriasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct);
    Task<CategoriaAdministracionDto> ObtenerAsync(int id, CancellationToken ct);
    Task<CategoriaAdministracionDto> CrearAsync(GuardarCategoriaRequest request, CancellationToken ct);
    Task<CategoriaAdministracionDto> ActualizarAsync(int id, GuardarCategoriaRequest request, CancellationToken ct);
    Task CambiarEstadoAsync(int id, CambiarEstadoCategoriaRequest request, CancellationToken ct);
    Task<Categoria> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct);
    Task<CategoriasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct);
}
