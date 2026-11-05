using NexaBack.Infrastructure.Data.Dtos.Comercial;

namespace NexaBack.Common;

// Validaciones compartidas de campos; cada módulo conserva su servicio y repositorio.
internal static class ContactoComercialDatos
{
    public static GuardarContactoRequest Normalizar(GuardarContactoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TipoDocumentoId <= 0) throw new BusinessException("Seleccioná el tipo de documento.");
        return request with {
            NombreRazonSocial = Validation.Required(request.NombreRazonSocial, "Nombre o razón social", 300),
            Documento = Validation.Required(request.Documento, "Documento / RUC", 30),
            Correo = Validation.Email(request.Correo),
            Telefono = Validation.Optional(request.Telefono, "Teléfono", 30),
            Direccion = Validation.Optional(request.Direccion, "Dirección", 300),
            Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500)
        };
    }
}
