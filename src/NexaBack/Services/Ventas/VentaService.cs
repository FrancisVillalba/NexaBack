using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Ventas;
using NexaBack.Repository.Ventas;
namespace NexaBack.Services.Ventas;

public sealed class VentaService(IVentaRepository repository) : IVentaService
{
    public Task<VentasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
        if (estado is not (null or "Confirmada" or "Anulada")) throw new BusinessException("Estado de venta inválido.");
        return repository.ListarAsync(pagina, tamanoPagina, Validation.Optional(buscar, "Búsqueda", 200), estado, ct);
    }
    public Task<VentaDto> ObtenerAsync(int id, CancellationToken ct) => repository.ObtenerAsync(id, ct);
    public Task<VentaCotizacionDto> CotizarAsync(VentaCotizarRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validar(request.ClienteId, request.DepositoId, request.Descuento, request.Items);
        return repository.CotizarAsync(request, ct);
    }
    public Task<VentaDto> ConfirmarAsync(VentaConfirmarRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperacionId == Guid.Empty || request.Items is null || request.Items.Any(x => x is null || x.VersionProducto == Guid.Empty))
            throw new BusinessException("Identificá la operación y calculá el total antes de confirmar.");
        Validar(request.ClienteId, request.DepositoId, request.Descuento, request.Items.Select(x => new VentaLineaSolicitud(x.ProductoId, x.Cantidad)).ToArray());
        var datos = request with { Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500), Items = request.Items.OrderBy(x => x.ProductoId).ToArray() };
        // Mismo contenido y usuario + mismo UUID: reintento seguro sin otra salida de stock.
        var canonico = JsonSerializer.Serialize(new { datos.ClienteId, datos.DepositoId, Descuento = Numero(datos.Descuento), datos.Observaciones,
            Items = datos.Items.Select(x => new { x.ProductoId, Cantidad = Numero(x.Cantidad), x.VersionProducto }) });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
        return repository.ConfirmarAsync(datos, hash, ct);
    }
    public Task<VentaDto> AnularAsync(int id, VentaAnularRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Version == Guid.Empty) throw new BusinessException("Falta la versión de la venta.");
        return repository.AnularAsync(id, request with { Motivo = Validation.Required(request.Motivo, "Motivo de anulación", 300) }, ct);
    }
    public Task<IReadOnlyList<VentaClienteDto>> ClientesAsync(string buscar, CancellationToken ct) => repository.ClientesAsync(Filtro(buscar), ct);
    public Task<IReadOnlyList<VentaProductoDto>> ProductosAsync(int depositoId, string buscar, CancellationToken ct)
    {
        if (depositoId <= 0) throw new BusinessException("Seleccioná un depósito.");
        return repository.ProductosAsync(depositoId, Filtro(buscar), ct);
    }
    public Task<IReadOnlyList<VentaDepositoDto>> DepositosAsync(CancellationToken ct) => repository.DepositosAsync(ct);
    private static string Numero(decimal numero) => numero.ToString("G29", CultureInfo.InvariantCulture);
    private static string Filtro(string buscar)
    {
        var valor = Validation.Required(buscar, "Búsqueda", 100);
        if (valor.Length < 2) throw new BusinessException("Escribí al menos dos caracteres.");
        return valor;
    }
    private static void Validar(int cliente, int deposito, decimal descuento, IReadOnlyList<VentaLineaSolicitud> items)
    {
        if (cliente <= 0 || deposito <= 0) throw new BusinessException("Seleccioná un cliente y un depósito.");
        if (items is null || items.Count is < 1 or > 200 || items.Any(x => x is null || x.ProductoId <= 0)
            || items.Select(x => x.ProductoId).Distinct().Count() != items.Count)
            throw new BusinessException("La venta debe tener entre 1 y 200 productos válidos, sin repeticiones.");
        foreach (var item in items) VentaReglas.Cantidad(item.Cantidad, "metro");
        VentaReglas.Descuento(descuento);
    }
}
