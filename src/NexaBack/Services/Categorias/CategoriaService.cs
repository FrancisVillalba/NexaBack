using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
using NexaBack.Infrastructure.Data.Dtos.Categorias;
using NexaBack.Repository.Categorias;

namespace NexaBack.Services.Categorias;

public sealed class CategoriaService(ICategoriaRepository repository) : ICategoriaService
{
    public async Task<CategoriasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, Validation.Optional(buscar, "Búsqueda", 200), activo, ct);
        return new(items.Select(Map).ToArray(), total, pagina, size);
    }

    public async Task<CategoriaAdministracionDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await repository.ObtenerAsync(id, false, ct) ?? throw new BusinessException("Categoría no encontrada.", 404));

    public async Task<CategoriaAdministracionDto> CrearAsync(GuardarCategoriaRequest request, CancellationToken ct)
    {
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        if (await repository.ExisteNombreAsync(nombre, null, ct)) throw new BusinessException("Ya existe una categoría con ese nombre, incluso entre las inactivas.", 409);
        var categoria = new Categoria { Nombre = nombre, Descripcion = descripcion };
        await repository.AgregarAsync(categoria, ct);
        return Map(categoria);
    }

    public async Task<CategoriaAdministracionDto> ActualizarAsync(int id, GuardarCategoriaRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        var categoria = await repository.ObtenerAsync(id, true, ct) ?? throw new BusinessException("Categoría no encontrada.", 404);
        if (categoria.Version != request.Version) throw new BusinessException("La categoría cambió mientras la editabas. Actualizá el listado.", 409);
        if (await repository.ExisteNombreAsync(nombre, id, ct)) throw new BusinessException("Ya existe una categoría con ese nombre.", 409);
        // El formulario no cambia el estado ni los campos de auditoría.
        categoria.Nombre = nombre;
        categoria.Descripcion = descripcion;
        categoria.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
        return Map(categoria);
    }

    public Task CambiarEstadoAsync(int id, CambiarEstadoCategoriaRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        return repository.CambiarEstadoAsync(id, request.Activo, request.Version, ct);
    }

    public async Task<Categoria> ValidarSeleccionAsync(int id, int? actualId, CancellationToken ct)
    {
        if (id <= 0) throw new BusinessException("Seleccioná una categoría.");
        var categoria = await repository.ObtenerAsync(id, true, ct) ?? throw new BusinessException("La categoría no existe.");
        // Desactivar conserva referencias anteriores; impide nuevas asignaciones.
        if (!categoria.Activo && actualId != id) throw new BusinessException("La categoría está inactiva.");
        return categoria;
    }
    public async Task<CategoriasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, null, null, ct);
        return new(items.Select(x => new CategoriaOpcionDto(x.Id, x.Nombre, x.Activo)).ToArray(), total, pagina, size);
    }

    private static void ValidarVersion(Guid? version)
    {
        if (version is null || version == Guid.Empty) throw new BusinessException("Falta la versión de la categoría. Actualizá el listado.");
    }

    private static CategoriaAdministracionDto Map(Categoria x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
