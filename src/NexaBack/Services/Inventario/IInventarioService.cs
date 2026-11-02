using NexaBack.Infrastructure.Data.Dtos.Inventario;

namespace NexaBack.Services.Inventario;

public interface IInventarioService
{
    Task<IReadOnlyList<DepositoDto>> DepositosAsync(CancellationToken ct);
    Task<InventarioPaginaDto> ListarAsync(int depositoId, int pagina, int tamanoPagina, string? buscar, string estado, bool? activo, CancellationToken ct);
    Task<MovimientosPaginaDto> MovimientosAsync(int? depositoId, int? productoId, int pagina, int tamanoPagina, string? buscar, CancellationToken ct);
    Task<MovimientoDto> RegistrarAsync(RegistrarMovimientoRequest request, CancellationToken ct);
}
