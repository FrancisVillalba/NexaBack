using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Cobros;
using NexaBack.Repository.Cobros;
namespace NexaBack.Services.Cobros;

public sealed class CobroService(ICobroRepository repository) : ICobroService
{
    public Task<VentaCobrosDto> ListarAsync(int ventaId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        VentaValida(ventaId); Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
        return repository.ListarAsync(ventaId, pagina, tamanoPagina, ct);
    }
    public Task<CobroDto> RegistrarAsync(int ventaId, RegistrarCobroRequest request, CancellationToken ct)
    {
        VentaValida(ventaId); ArgumentNullException.ThrowIfNull(request);
        if (request.OperacionId == Guid.Empty || request.VersionVenta == Guid.Empty) throw new BusinessException("Falta identificar la operación o la versión de la venta.");
        CobroReglas.ValidarImporte(request.Importe);
        if (request.FechaCobro == default) throw new BusinessException("La fecha del cobro es obligatoria.");
        var medio = Validation.Required(request.MedioPago, "Medio de pago", 20).ToUpperInvariant();
        if (medio is not ("EFECTIVO" or "TRANSFERENCIA" or "TARJETA" or "OTRO")) throw new BusinessException("Seleccioná un medio de pago válido.");
        var datos = request with { MedioPago = medio, Referencia = Validation.Optional(request.Referencia, "Referencia", 100), Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500) };
        // El importe y la fecha forman parte de la identidad del cobro; la versión es solo una precondición.
        var canonico = JsonSerializer.Serialize(new { VentaId = ventaId, Importe = datos.Importe.ToString("G29", CultureInfo.InvariantCulture),
            datos.FechaCobro, datos.MedioPago, datos.Referencia, datos.Observaciones });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
        return repository.RegistrarAsync(ventaId, datos, hash, ct);
    }
    public Task<CobroDto> AnularAsync(int ventaId, int cobroId, AnularCobroRequest request, CancellationToken ct)
    {
        VentaValida(ventaId); ArgumentNullException.ThrowIfNull(request);
        if (cobroId <= 0 || request.Version == Guid.Empty) throw new BusinessException("Identificá el cobro y su versión.");
        return repository.AnularAsync(ventaId, cobroId, request with { Motivo = Validation.Required(request.Motivo, "Motivo de anulación", 300) }, ct);
    }
    private static void VentaValida(int ventaId) { if (ventaId <= 0) throw new BusinessException("Venta inválida."); }
}
