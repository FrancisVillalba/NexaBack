using NexaBack.Infrastructure.Data.Dtos.Inventario;

namespace NexaBack.Services.Inventario;

public interface IDepositoService
{
    Task<DepositosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct);
    Task<DepositoAdministracionDto> ObtenerAsync(int id, CancellationToken ct);
    Task<DepositoAdministracionDto> CrearAsync(GuardarDepositoRequest request, CancellationToken ct);
    Task<DepositoAdministracionDto> ActualizarAsync(int id, GuardarDepositoRequest request, CancellationToken ct);
    Task CambiarEstadoAsync(int id, CambiarEstadoDepositoRequest request, CancellationToken ct);
}
