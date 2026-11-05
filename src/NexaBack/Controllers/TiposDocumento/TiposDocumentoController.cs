using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.TiposDocumento;
using NexaBack.Services.TiposDocumento;
namespace NexaBack.Controllers.TiposDocumento;

[ApiController, Authorize(Policy = "tipos-documento.consultar"), Route("api/tipos-documento")]
public sealed class TiposDocumentoController(ITipoDocumentoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TipoDocumentoDto>>> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));
}
