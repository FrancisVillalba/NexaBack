using NexaBack.Common;
using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
using NexaBack.Infrastructure.Data.Dtos.TiposDocumento;
using NexaBack.Repository.TiposDocumento;
namespace NexaBack.Services.TiposDocumento;

public sealed class TipoDocumentoService(ITipoDocumentoRepository repository) : ITipoDocumentoService
{
    public async Task<IReadOnlyList<TipoDocumentoDto>> ListarAsync(CancellationToken ct)
        => (await repository.ListarAsync(ct)).Select(x => new TipoDocumentoDto(x.Id, x.Codigo, x.Nombre, x.Activo)).ToArray();
    public async Task<TipoDocumento> ValidarAsync(int id, int? actualId, CancellationToken ct)
    {
        if (id <= 0) throw new BusinessException("Seleccioná el tipo de documento.");
        var tipo = await repository.ObtenerAsync(id, ct) ?? throw new BusinessException("El tipo de documento no existe.");
        // Permite conservar el tipo inactivo de un registro, pero nunca asignarlo a uno nuevo.
        if (!tipo.Activo && actualId != id) throw new BusinessException("El tipo de documento está inactivo. Seleccioná uno vigente.");
        return tipo;
    }
}
