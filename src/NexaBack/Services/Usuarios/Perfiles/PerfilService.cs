using NexaBack.Repository.Usuarios.Perfiles;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Perfiles;

namespace NexaBack.Services.Usuarios.Perfiles;

public sealed class PerfilService(IPerfilRepository repository) : IPerfilService
{
    // Solo Administrador es un rol protegido. Los demás acceden por sus permisos y PerfilId.
    // La protección también está en el servidor: no depende del formulario de React.
    private static readonly HashSet<string> NombresProtegidos = new(StringComparer.Ordinal)
        { "Administrador" };

    public async Task<IReadOnlyList<PerfilDto>> ListarAsync(
        int pagina = 1, int tamanoPagina = 20, string? buscar = null, bool? activo = null, CancellationToken cancellationToken = default)
    {
        var size = Validation.PageSize(tamanoPagina);
        var offset = Validation.Offset(pagina, size);
        var filtro = Validation.Optional(buscar, "Búsqueda", 200);
        var entities = await repository.ListarAsync(offset, size, filtro, activo, cancellationToken);
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

    public async Task<PerfilDto> ActualizarAsync(int id, ActualizarPerfilRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var perfil = await repository.ObtenerParaActualizarAsync(id, cancellationToken)
            ?? throw new BusinessException("Perfil no encontrado.", 404);
        var nombre = Validation.Required(request.Nombre, "Nombre", 50);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 250);
        if (NombresProtegidos.Contains(perfil.Nombre) && nombre != perfil.Nombre)
            throw new BusinessException("El nombre de este perfil está protegido porque determina sus accesos al sistema.", 409);
        if (await repository.ExisteNombreAsync(nombre, cancellationToken, excluirId: id))
            throw new BusinessException("Ya existe un perfil con ese nombre.", 409);

        // La edición no cambia el estado ni la fecha de creación.
        perfil.Nombre = nombre;
        perfil.Descripcion = descripcion;
        await repository.GuardarCambiosAsync(cancellationToken);
        return Map(perfil);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken cancellationToken = default)
    {
        var perfil = await repository.ObtenerParaActualizarAsync(id, cancellationToken)
            ?? throw new BusinessException("Perfil no encontrado.", 404);
        // Este perfil engloba a TODOS los administradores; su baja bloquearía la administración.
        if (!activo && perfil.Nombre == "Administrador")
            throw new BusinessException("El perfil Administrador no se puede desactivar.", 409);
        perfil.Activo = activo;
        await repository.GuardarCambiosAsync(cancellationToken);
    }

    private static PerfilDto Map(Perfil x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo, x.CreadoEn);
}
