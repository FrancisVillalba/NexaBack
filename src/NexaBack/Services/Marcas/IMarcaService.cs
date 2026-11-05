using NexaBack.Infrastructure.Data.Context.Marcas.Entity;
using NexaBack.Infrastructure.Data.Dtos.Marcas;

namespace NexaBack.Services.Marcas;

public interface IMarcaService
{
    Task<MarcasPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct);
    Task<MarcaAdministracionDto> ObtenerAsync(int id, CancellationToken ct);
    Task<MarcaAdministracionDto> CrearAsync(GuardarMarcaRequest request, CancellationToken ct);
    Task<MarcaAdministracionDto> ActualizarAsync(int id, GuardarMarcaRequest request, CancellationToken ct);
    Task CambiarEstadoAsync(int id, CambiarEstadoMarcaRequest request, CancellationToken ct);
    Task<Marca?> ValidarSeleccionAsync(int? id, int? actualId, CancellationToken ct);
    Task<MarcasOpcionesPaginaDto> OpcionesAsync(int pagina, int tamanoPagina, CancellationToken ct);
}
