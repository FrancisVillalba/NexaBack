using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Presupuestos;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Repository.Presupuestos;
using NexaBack.Services.Ventas;
namespace NexaBack.Services.Presupuestos;

public sealed class PresupuestoService(IPresupuestoRepository repository) : IPresupuestoService
{
    public Task<PresupuestosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
        if (estado is not (null or "Borrador" or "Aprobado" or "Cancelado" or "Convertido" or "Vencido")) throw new BusinessException("Estado de presupuesto inválido.");
        return repository.ListarAsync(pagina, tamanoPagina, Validation.Optional(buscar, "Búsqueda", 200), estado, ct);
    }
    public Task<PresupuestoDto> ObtenerAsync(int id, CancellationToken ct) { Id(id); return repository.ObtenerAsync(id, ct); }
    public Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validar(request.ClienteId, request.DepositoId, request.Descuento, request.Items);
        return repository.CotizarAsync(request, ct);
    }
    public Task<PresupuestoDto> GuardarAsync(int? id, PresupuestoGuardarRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id.HasValue) Id(id.Value);
        if (request.OperacionId == Guid.Empty || id.HasValue && (!request.Version.HasValue || request.Version == Guid.Empty))
            throw new BusinessException("Falta identificar la operación o la versión del presupuesto.");
        if (request.Items is null || request.Items.Any(x => x is null || x.VersionProducto == Guid.Empty)) throw new BusinessException("Calculá el total antes de guardar.");
        Validar(request.ClienteId, request.DepositoId, request.Descuento, request.Items.Select(x => new VentaLineaSolicitud(x.ProductoId, x.Cantidad)).ToArray());
        // La fecha se comprueba al escribir, después de resolver posibles reintentos ya confirmados.
        var datos = request with { Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500), Items = request.Items.OrderBy(x => x.ProductoId).ToArray() };
        var hash = Hash(new
        {
            Id = id,
            datos.ClienteId,
            datos.DepositoId,
            datos.FechaVencimiento,
            Descuento = Numero(datos.Descuento),
            datos.Observaciones,
            Items = datos.Items.Select(x => new { x.ProductoId, Cantidad = Numero(x.Cantidad), x.VersionProducto })
        });
        return repository.GuardarAsync(id, datos, hash, ct);
    }
    public Task<PresupuestoDto> AprobarAsync(int id, Guid version, CancellationToken ct) { Id(id); Version(version); return repository.AprobarAsync(id, version, ct); }
    public Task<PresupuestoDto> CancelarAsync(int id, PresupuestoCancelarRequest request, CancellationToken ct)
    {
        Id(id); ArgumentNullException.ThrowIfNull(request); Version(request.Version);
        return repository.CancelarAsync(id, request with { Motivo = Validation.Required(request.Motivo, "Motivo de cancelación", 300) }, ct);
    }
    public Task<PresupuestoConversionDto> PrepararConversionAsync(int id, CancellationToken ct) { Id(id); return repository.PrepararConversionAsync(id, ct); }
    public Task<VentaDto> ConvertirAsync(int id, PresupuestoConvertirRequest request, CancellationToken ct)
    {
        Id(id); ArgumentNullException.ThrowIfNull(request); Version(request.Version);
        if (request.OperacionId == Guid.Empty || request.Items is null || request.Items.Count is < 1 or > 200
            || request.Items.Any(x => x is null || x.ProductoId <= 0 || x.VersionProducto == Guid.Empty)
            || request.Items.Select(x => x.ProductoId).Distinct().Count() != request.Items.Count)
            throw new BusinessException("Revisá la cotización de conversión antes de confirmar.");
        var datos = request with { Items = request.Items.OrderBy(x => x.ProductoId).ToArray() };
        return repository.ConvertirAsync(id, datos, Hash(new { PresupuestoId = id, datos.Version, datos.Items }), ct);
    }
    private static void Validar(int cliente, int deposito, decimal descuento, IReadOnlyList<VentaLineaSolicitud> items)
    {
        if (cliente <= 0 || deposito <= 0) throw new BusinessException("Seleccioná cliente y depósito.");
        if (items is null || items.Count is < 1 or > 200 || items.Any(x => x is null || x.ProductoId <= 0)
            || items.Select(x => x.ProductoId).Distinct().Count() != items.Count) throw new BusinessException("Agregá entre 1 y 200 productos sin repeticiones.");
        foreach (var item in items) VentaReglas.Cantidad(item.Cantidad, "metro");
        VentaReglas.Descuento(descuento);
    }
    private static void Id(int id) { if (id <= 0) throw new BusinessException("Presupuesto inválido."); }
    private static void Version(Guid version) { if (version == Guid.Empty) throw new BusinessException("Falta la versión del presupuesto."); }
    private static string Numero(decimal n) => n.ToString("G29", CultureInfo.InvariantCulture);
    private static string Hash(object datos) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos))));
}
