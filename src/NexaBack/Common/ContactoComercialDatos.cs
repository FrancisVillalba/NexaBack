using NexaBack.Infrastructure.Data.Dtos.Comercial;

namespace NexaBack.Common;

// Validaciones compartidas de campos; cada módulo conserva su servicio y repositorio.
internal static class ContactoComercialDatos
{
    public static GuardarContactoRequest Normalizar(GuardarContactoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PlazoPagoDias is < 0 or > 365) throw new BusinessException("El plazo de pago debe estar entre 0 y 365 días.");
        if (request.LimiteCredito < 0 || request.LimiteCredito > 999999999999.99m || decimal.Round(request.LimiteCredito, 2) != request.LimiteCredito)
            throw new BusinessException("El límite de crédito admite hasta 12 enteros y 2 decimales y no puede ser negativo.");
        var tipo = Validation.Required(request.TipoDocumento, "Tipo de documento", 20).ToUpperInvariant();
        if (tipo is not ("CI" or "RUC" or "PASAPORTE" or "OTRO"))
            throw new BusinessException("Seleccioná un tipo de documento válido: CI, RUC, Pasaporte u Otro.");
        return request with {
            TipoDocumento = tipo,
            NombreRazonSocial = Validation.Required(request.NombreRazonSocial, "Nombre o razón social", 300),
            Documento = Validation.Required(request.Documento, "Documento / RUC", 30),
            Correo = Validation.Email(request.Correo),
            Telefono = Validation.Optional(request.Telefono, "Teléfono", 30),
            Direccion = Validation.Optional(request.Direccion, "Dirección", 300),
            Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500)
        };
    }
}
