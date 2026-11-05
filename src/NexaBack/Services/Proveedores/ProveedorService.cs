using NexaBack.Services.TiposDocumento;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Dtos.Comercial;
using NexaBack.Repository.Proveedores;

namespace NexaBack.Services.Proveedores;

// El nombre o razón social admite tanto particulares como empresas, sin exigir apellidos.
public sealed class ProveedorService(IProveedorRepository repository, ITipoDocumentoService tipos) : IProveedorService
{
    public async Task<ContactosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct)
    {
        var size = Validation.PageSize(tamanoPagina);
        var (items, total) = await repository.ListarAsync(Validation.Offset(pagina, size), size, Validation.Optional(buscar, "Búsqueda", 200), activo, ct);
        return new(items.Select(Map).ToArray(), total, pagina, size);
    }
    public async Task<ContactoComercialDto> ObtenerAsync(int id, CancellationToken ct) => Map(await ObtenerEntidadAsync(id, false, ct));
    public async Task<ContactoComercialDto> CrearAsync(GuardarContactoRequest request, CancellationToken ct)
    {
        var datos = Validar(request);
        var tipo = await tipos.ValidarAsync(datos.TipoDocumentoId, null, ct);
        await ValidarDocumentoAsync(datos.TipoDocumentoId, datos.Documento, null, ct);
        var contacto = new Proveedor { NombreRazonSocial = datos.NombreRazonSocial, Documento = datos.Documento };
        Aplicar(contacto, datos);
        contacto.TipoDocumento = tipo;
        await repository.AgregarAsync(contacto, ct);
        return Map(contacto);
    }
    public async Task<ContactoComercialDto> ActualizarAsync(int id, GuardarContactoRequest request, CancellationToken ct)
    {
        var datos = Validar(request);
        var contacto = await ObtenerEntidadAsync(id, true, ct);
        ValidarVersion(contacto, datos.Version);
        var tipo = await tipos.ValidarAsync(datos.TipoDocumentoId, contacto.TipoDocumentoId, ct);
        await ValidarDocumentoAsync(datos.TipoDocumentoId, datos.Documento, id, ct);
        // Editar un cliente/proveedor no modifica ninguna Persona ni el otro módulo.
        Aplicar(contacto, datos);
        contacto.TipoDocumento = tipo;
        contacto.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
        return Map(contacto);
    }
    public async Task CambiarEstadoAsync(int id, CambiarEstadoContactoRequest request, CancellationToken ct)
    {
        var contacto = await ObtenerEntidadAsync(id, true, ct);
        ValidarVersion(contacto, request.Version);
        if (contacto.Activo == request.Activo) return;
        contacto.Activo = request.Activo;
        contacto.Version = Guid.NewGuid();
        await repository.GuardarCambiosAsync(ct);
    }
    private async Task ValidarDocumentoAsync(int tipoDocumentoId, string documento, int? excluirId, CancellationToken ct)
    {
        if (await repository.ExisteDocumentoAsync(tipoDocumentoId, documento, ct, excluirId))
            throw new BusinessException("Ya existe un proveedor con ese tipo y número de documento, o uno anterior sin tipo definido. Editá o reactivá el registro existente.", 409);
    }
    private async Task<Proveedor> ObtenerEntidadAsync(int id, bool actualizar, CancellationToken ct)
        => await repository.ObtenerAsync(id, actualizar, ct) ?? throw new BusinessException("Registro no encontrado.", 404);
    private static GuardarContactoRequest Validar(GuardarContactoRequest request)
    {
        var datos = ContactoComercialDatos.Normalizar(request);
        return datos;
    }
    private static void ValidarVersion(Proveedor contacto, Guid? version)
    {
        if (version is null || version == Guid.Empty || version != contacto.Version)
            throw new BusinessException("El registro cambió o falta su versión. Actualizá el listado.", 409);
    }
    private static void Aplicar(Proveedor contacto, GuardarContactoRequest datos)
    {
        contacto.NombreRazonSocial = datos.NombreRazonSocial;
        contacto.TipoDocumentoId = datos.TipoDocumentoId;
        contacto.Documento = datos.Documento;
        contacto.Correo = datos.Correo;
        contacto.Telefono = datos.Telefono;
        contacto.Direccion = datos.Direccion;
        contacto.Observaciones = datos.Observaciones;
    }
    private static ContactoComercialDto Map(Proveedor x) => new(x.Id, x.NombreRazonSocial, x.TipoDocumentoId, x.TipoDocumento?.Nombre, x.Documento, x.Correo,
        x.Telefono, x.Direccion, x.Observaciones, x.Activo, x.Version,
        x.CreadoEn, x.CreadoPor, x.ModificadoEn, x.ModificadoPor);
}
