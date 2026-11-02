using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Dtos.Inventario;
using NexaBack.Repository.Inventario;

namespace NexaBack.Services.Inventario;

public sealed class DepositoService(IDepositoRepository repository) : IDepositoService
{
    public async Task<DepositosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, Validation.Optional(buscar, "Búsqueda", 200), activo, ct);
        return new(items.Select(Map).ToArray(), total, pagina, size);
    }

    public async Task<DepositoAdministracionDto> ObtenerAsync(int id, CancellationToken ct)
        => Map(await repository.ObtenerAsync(id, false, ct) ?? throw new BusinessException("Depósito no encontrado.", 404));

    public async Task<DepositoAdministracionDto> CrearAsync(GuardarDepositoRequest request, CancellationToken ct)
    {
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        if (await repository.ExisteNombreAsync(nombre, null, ct)) throw new BusinessException("Ya existe un depósito con ese nombre, incluso entre los inactivos.", 409);
        var deposito = new Deposito { Nombre = nombre, Descripcion = descripcion };
        await repository.AgregarAsync(deposito, ct);
        return Map(deposito);
    }

    public async Task<DepositoAdministracionDto> ActualizarAsync(int id, GuardarDepositoRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        var nombre = Validation.Required(request.Nombre, "Nombre", 100);
        var descripcion = Validation.Optional(request.Descripcion, "Descripción", 300);
        var deposito = await repository.ObtenerAsync(id, true, ct) ?? throw new BusinessException("Depósito no encontrado.", 404);
        if (deposito.Version != request.Version) throw new BusinessException("El depósito cambió mientras lo editabas. Actualizá el listado.", 409);
        if (await repository.ExisteNombreAsync(nombre, id, ct)) throw new BusinessException("Ya existe un depósito con ese nombre.", 409);
        // El formulario no cambia el estado ni los campos de auditoría.
        deposito.Nombre = nombre;
        deposito.Descripcion = descripcion;
        deposito.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
        return Map(deposito);
    }

    public Task CambiarEstadoAsync(int id, CambiarEstadoDepositoRequest request, CancellationToken ct)
    {
        ValidarVersion(request.Version);
        return repository.CambiarEstadoAsync(id, request.Activo, request.Version, ct);
    }

    private static void ValidarVersion(Guid? version)
    {
        if (version is null || version == Guid.Empty) throw new BusinessException("Falta la versión del depósito. Actualizá el listado.");
    }

    private static DepositoAdministracionDto Map(Deposito x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
