using NexaBack.Infrastructure.Data.Dtos.Compras;
namespace NexaBack.Repository.Compras;

public interface ICompraRepository
{
    Task<ComprasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, string? estado, CancellationToken ct);
    Task<CompraDto> ObtenerAsync(int id, CancellationToken ct);
    Task<CompraDto> CrearAsync(CrearCompraRequest request, string solicitudHash, CancellationToken ct);
    Task<CompraDto> EditarAsync(int id, CompraDatosRequest datos, Guid version, CancellationToken ct);
    Task<CompraDto> RecibirAsync(int id, Guid version, CancellationToken ct);
    Task<CompraDto> CancelarAsync(int id, Guid version, CancellationToken ct);
    Task<IReadOnlyList<CompraProveedorDto>> ProveedoresAsync(string buscar, CancellationToken ct);
    Task<IReadOnlyList<CompraProductoDto>> ProductosAsync(string buscar, CancellationToken ct);
    Task<IReadOnlyList<CompraDepositoDto>> DepositosAsync(CancellationToken ct);
}
