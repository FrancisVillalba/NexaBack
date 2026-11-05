using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Marcas.Entity;
using NexaBack.Infrastructure.Data.Dtos.Marcas;
using NexaBack.Repository.Marcas;

namespace NexaBack.Services.Marcas;

public sealed class MarcaService(IMarcaRepository repository) : IMarcaService
{
    public async Task<MarcasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, Validation.Optional(buscar, "Búsqueda", 200), activo, ct);
        return new(items.Select(Map).ToArray(), total, pagina, size);
    }

    public async Task<MarcaAdministracionDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await repository.ObtenerAsync(id, false, ct) ?? throw new BusinessException("Marca no encontrada.", 404));

    public async Task<MarcaAdministracionDto> CrearAsync(GuardarMarcaRequest request, CancellationToken ct)
    {
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        if (await repository.ExisteNombreAsync(nombre, null, ct)) throw new BusinessException("Ya existe una marca con ese nombre, incluso entre las inactivas.", 409);
        var marca = new Marca { Nombre = nombre, Descripcion = descripcion };
        await repository.AgregarAsync(marca, ct);
        return Map(marca);
    }

    public async Task<MarcaAdministracionDto> ActualizarAsync(int id, GuardarMarcaRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        var marca = await repository.ObtenerAsync(id, true, ct) ?? throw new BusinessException("Marca no encontrada.", 404);
        if (marca.Version != request.Version) throw new BusinessException("La marca cambió mientras la editabas. Actualizá el listado.", 409);
        if (await repository.ExisteNombreAsync(nombre, id, ct)) throw new BusinessException("Ya existe una marca con ese nombre.", 409);
        // El formulario no cambia el estado ni los campos de auditoría.
        marca.Nombre = nombre;
        marca.Descripcion = descripcion;
        marca.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
        return Map(marca);
    }

    public Task CambiarEstadoAsync(int id, CambiarEstadoMarcaRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        return repository.CambiarEstadoAsync(id, request.Activo, request.Version, ct);
    }

    public async Task<Marca?> ValidarSeleccionAsync(int? id, int? actualId, CancellationToken ct)
    {
        if (!id.HasValue) return null;
        if (id <= 0) throw new BusinessException("Seleccioná una marca válida o Sin marca.");
        var marca = await repository.ObtenerAsync(id.Value, true, ct) ?? throw new BusinessException("La marca no existe.");
        if (!marca.Activo && actualId != id) throw new BusinessException("La marca está inactiva. Seleccioná una marca vigente o Sin marca.");
        return marca;
    }
    public async Task<MarcasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, null, null, ct);
        return new(items.Select(x => new MarcaOpcionDto(x.Id, x.Nombre, x.Activo)).ToArray(), total, pagina, size);
    }

    private static void ValidarVersion(Guid? version)
    {
        if (version is null || version == Guid.Empty) throw new BusinessException("Falta la versión de la marca. Actualizá el listado.");
    }

    private static MarcaAdministracionDto Map(Marca x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
