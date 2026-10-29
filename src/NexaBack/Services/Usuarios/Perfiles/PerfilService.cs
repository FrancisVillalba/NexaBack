using NexaBack.Repository.Usuarios.Perfiles;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;

namespace NexaBack.Services.Usuarios.Perfiles;

public sealed class PerfilService(IPerfilRepository repository) : IPerfilService
{
    public async Task<IReadOnlyList<PerfilDto>> ListarAsync(
        int pagina = 1, int tamanoPagina = 20, CancellationToken cancellationToken = default)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var entities = await repository.ListarAsync(offset, size, cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<PerfilDto> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.ObtenerAsync(id, cancellationToken)
            ?? throw new BusinessException("Perfil no encontrado.", 404);
        return Map(entity);
    }

    public async Task<PerfilDto> CrearAsync(CrearPerfilRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nombre = Validation.Required(request.Nombre, "Nombre", 50);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 250);
        if (await repository.ExisteNombreAsync(nombre, cancellationToken))
            throw new BusinessException("Ya existe un perfil con ese nombre.", 409);
        var entity = new Perfil { Nombre = nombre, Descripcion = descripcion };
        await repository.AgregarAsync(entity, cancellationToken);
        return Map(entity);
    }

    private static PerfilDto Map(Perfil x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo, x.CreadoEn);
}
