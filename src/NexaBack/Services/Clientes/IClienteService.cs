using NexaBack.Infrastructure.Data.Dtos.Comercial;
namespace NexaBack.Services.Clientes;

public interface IClienteService
{
    Task<ContactosPaginaDto> ListarAsync(int pagina, int tamanoPagina, string? buscar, bool? activo, CancellationToken ct);
    Task<ContactoComercialDto> ObtenerAsync(int id, CancellationToken ct);
    Task<ContactoComercialDto> CrearAsync(GuardarContactoRequest request, CancellationToken ct);
    Task<ContactoComercialDto> ActualizarAsync(int id, GuardarContactoRequest request, CancellationToken ct);
    Task CambiarEstadoAsync(int id, CambiarEstadoContactoRequest request, CancellationToken ct);
}
