using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Inventario;
using NexaBack.Repository.Inventario;

namespace NexaBack.Services.Inventario;

public sealed class InventarioService(IInventarioRepository repository) : IInventarioService
{
    public Task<IReadOnlyList<DepositoDto>> DepositosAsync(CancellationToken ct) => repository.DepositosAsync(ct);

    public Task<InventarioPaginaDto> ListarAsync(int depositoId, int pagina, int tamanoPagina, string? buscar, string estado, bool? activo, CancellationToken ct)
    {
        ValidarPagina(pagina, tamanoPagina);
        if (depositoId <= 0) throw new BusinessException("Seleccioná un depósito.");
        if (estado is not ("todos" or "bajo" or "sin_stock" or "disponible")) throw new BusinessException("El filtro de existencias no es válido.");
        return repository.ListarAsync(depositoId, pagina, tamanoPagina, Validation.Optional(buscar, "Búsqueda", 200), estado, activo, ct);
    }

    public Task<MovimientosPaginaDto> MovimientosAsync(int? depositoId, int? productoId, int pagina, int tamanoPagina, string? buscar, CancellationToken ct)
    {
        ValidarPagina(pagina, tamanoPagina);
        if (depositoId is <= 0 || productoId is <= 0) throw new BusinessException("El producto o depósito no es válido.");
        return repository.MovimientosAsync(depositoId, productoId, pagina, tamanoPagina, Validation.Optional(buscar, "Búsqueda", 200), ct);
    }

    public Task<MovimientoDto> RegistrarAsync(RegistrarMovimientoRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperacionId == Guid.Empty || request.ProductoId <= 0 || request.DepositoId <= 0) throw new BusinessException("Falta identificar la operación, el producto o el depósito.");
        var tipo = Validation.Required(request.Tipo, "Tipo", 20).ToLowerInvariant();
        if (tipo is not ("entrada" or "salida" or "ajuste" or "transferencia")) throw new BusinessException("El tipo de movimiento no es válido.");
        if (tipo == "transferencia" && (request.DepositoDestinoId is null or <= 0 || request.DepositoDestinoId == request.DepositoId))
            throw new BusinessException("La transferencia requiere un depósito de destino diferente al de origen.");
        if (tipo != "transferencia" && request.DepositoDestinoId is not null) throw new BusinessException("Solo una transferencia lleva depósito de destino.");
        var motivo = Validation.Required(request.Motivo, "Motivo", 300);
        return repository.RegistrarAsync(request with { Tipo = tipo, Motivo = motivo }, ct);
    }

    private static void ValidarPagina(int pagina, int tamanoPagina)
        => Validation.Offset(pagina, Validation.PageSize(tamanoPagina));
}
