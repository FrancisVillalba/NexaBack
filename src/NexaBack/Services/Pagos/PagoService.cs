using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Pagos;
using NexaBack.Repository.Pagos;
namespace NexaBack.Services.Pagos;

public sealed class PagoService(IPagoRepository repository) : IPagoService
{
    public Task<CompraPagosDto> ListarAsync(int compraId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        CompraValida(compraId); Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
        return repository.ListarAsync(compraId, pagina, tamanoPagina, ct);
    }
    public Task<PagoDto> RegistrarAsync(int compraId, RegistrarPagoRequest request, CancellationToken ct)
    {
        CompraValida(compraId); ArgumentNullException.ThrowIfNull(request);
        if (request.OperacionId == Guid.Empty || request.VersionCompra == Guid.Empty) throw new BusinessException("Falta identificar la operación o la versión de la compra.");
        PagoReglas.ValidarImporte(request.Importe);
        if (request.FechaPago == default) throw new BusinessException("La fecha del pago es obligatoria.");
        var medio = Validation.Required(request.MedioPago, "Medio de pago", 20).ToUpperInvariant();
        if (medio is not ("EFECTIVO" or "TRANSFERENCIA" or "TARJETA" or "OTRO")) throw new BusinessException("Seleccioná un medio de pago válido.");
        var datos = request with { MedioPago = medio, Referencia = Validation.Optional(request.Referencia, "Referencia", 100), Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500) };
        // El importe y la fecha forman parte de la identidad del pago; la versión es solo una precondición.
        var canonico = JsonSerializer.Serialize(new { CompraId = compraId, Importe = datos.Importe.ToString("G29", CultureInfo.InvariantCulture),
            datos.FechaPago, datos.MedioPago, datos.Referencia, datos.Observaciones });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
        return repository.RegistrarAsync(compraId, datos, hash, ct);
    }
    public Task<PagoDto> AnularAsync(int compraId, int pagoId, AnularPagoRequest request, CancellationToken ct)
    {
        CompraValida(compraId); ArgumentNullException.ThrowIfNull(request);
        if (pagoId <= 0 || request.Version == Guid.Empty) throw new BusinessException("Identificá el pago y su versión.");
        return repository.AnularAsync(compraId, pagoId, request with { Motivo = Validation.Required(request.Motivo, "Motivo de anulación", 300) }, ct);
    }
    private static void CompraValida(int compraId) { if (compraId <= 0) throw new BusinessException("Compra inválida."); }
}
