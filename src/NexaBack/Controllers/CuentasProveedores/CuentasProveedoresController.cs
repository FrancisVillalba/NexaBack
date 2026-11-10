using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaBack.Infrastructure.Data.Dtos.CuentasProveedores;
using NexaBack.Infrastructure.Security;
using NexaBack.Services.CuentasProveedores;
namespace NexaBack.Controllers.CuentasProveedores;

[ApiController, Authorize, Route("api/proveedores/{proveedorId:int}/cuenta-corriente")]
public sealed class CuentasProveedoresController(ICuentaProveedorService service, UsuariosDbContext context) : ControllerBase
{
    [HttpGet, RequireAccess("proveedores.cuenta_corriente")]
    public async Task<ActionResult<CuentaProveedorDto>> Obtener(int proveedorId, int paginaCompras = 1, int paginaPagos = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        var resultado = await service.ObtenerAsync(proveedorId, paginaCompras, paginaPagos, tamanoPagina, ct);
        await context.CompletarNombresAsync(resultado.Pagos, ct);
        return Ok(resultado);
    }
}
