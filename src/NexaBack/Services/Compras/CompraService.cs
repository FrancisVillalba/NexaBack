using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Compras;
using NexaBack.Repository.Compras;

namespace NexaBack.Services.Compras;

public sealed class CompraService(ICompraRepository repository) : ICompraService
{
    public Task<ComprasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct)
    {
        Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
        if (estado is not (null or "Pendiente" or "Recibida" or "Cancelada")) throw new BusinessException("Estado de compra inválido.");
        return repository.ListarAsync(pagina, tamanoPagina, Validation.Optional(buscar, "Búsqueda", 200), estado, ct);
    }
    public Task<CompraDto> ObtenerAsync(int id, CancellationToken ct) => repository.ObtenerAsync(id, ct);
    public Task<CompraDto> CrearAsync(CrearCompraRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperacionId == Guid.Empty || request.ProveedorId <= 0 || request.DepositoId <= 0) throw new BusinessException("Seleccioná proveedor y depósito e identificá la operación.");
        var normalizado = Normalizar(request);
        var canonico = JsonSerializer.Serialize(new { normalizado.ProveedorId, normalizado.DepositoId, normalizado.Observaciones,
            Items = normalizado.Items.Select(x => new { x.ProductoId, Cantidad = x.Cantidad.ToString("G29", CultureInfo.InvariantCulture), Costo = x.CostoUnitario.ToString("G29", CultureInfo.InvariantCulture) }) });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonico)));
        return repository.CrearAsync(request with { Observaciones = normalizado.Observaciones, Items = normalizado.Items }, hash, ct);
    }
    public Task<CompraDto> EditarAsync(int id, EditarCompraRequest request, CancellationToken ct)
    {
        if (request is null || request.Version == Guid.Empty) throw new BusinessException("Falta la versión de la compra.");
        return repository.EditarAsync(id, Normalizar(request), request.Version, ct);
    }

    private static CompraDatosRequest Normalizar(CompraDatosRequest request)
    {
        if (request.ProveedorId <= 0 || request.DepositoId <= 0) throw new BusinessException("Seleccioná proveedor y depósito.");
        if (request.Items is null || request.Items.Count is < 1 or > 200) throw new BusinessException("La compra requiere entre 1 y 200 productos.");
        if (request.Items.Any(x => x is null || x.ProductoId <= 0) || request.Items.Select(x => x.ProductoId).Distinct().Count() != request.Items.Count)
            throw new BusinessException("Los productos deben ser válidos y no pueden repetirse.");
        // La unidad real se verifica en el repositorio. Aquí ya se comprueban rangos y precisión.
        foreach (var item in request.Items) CompraReglas.Subtotal(item.Cantidad, item.CostoUnitario, "metro");
        return request with { Observaciones = Validation.Optional(request.Observaciones, "Observaciones", 500), Items = request.Items.OrderBy(x => x.ProductoId).ToArray() };
    }

    public Task<CompraDto> RecibirAsync(int id, CompraEstadoRequest request, CancellationToken ct) => repository.RecibirAsync(id, Version(request), ct);
    public Task<CompraDto> CancelarAsync(int id, CompraEstadoRequest request, CancellationToken ct) => repository.CancelarAsync(id, Version(request), ct);
    public Task<IReadOnlyList<CompraProveedorDto>> ProveedoresAsync(string buscar, CancellationToken ct) => repository.ProveedoresAsync(Filtro(buscar), ct);
    public Task<IReadOnlyList<CompraProductoDto>> ProductosAsync(string buscar, CancellationToken ct) => repository.ProductosAsync(Filtro(buscar), ct);
    public Task<IReadOnlyList<CompraDepositoDto>> DepositosAsync(CancellationToken ct) => repository.DepositosAsync(ct);
    private static Guid Version(CompraEstadoRequest request)
    {
        if (request is null || request.Version == Guid.Empty) throw new BusinessException("Falta la versión de la compra.");
        return request.Version;
    }
    private static string Filtro(string buscar)
    {
        var filtro = Validation.Required(buscar, "Búsqueda", 100);
        if (filtro.Length < 2) throw new BusinessException("Escribí al menos dos caracteres.");
        return filtro;
    }
}
