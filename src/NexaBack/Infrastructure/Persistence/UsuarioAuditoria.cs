using System.Globalization;

namespace NexaBack.Infrastructure.Persistence;

// La identidad se obtiene del token validado por el servidor, nunca de los datos del formulario.
public sealed class UsuarioAuditoria(IHttpContextAccessor accessor) : IUsuarioAuditoria
{
    public int? UsuarioId => accessor.HttpContext?.User is { Identity.IsAuthenticated: true } usuario
        && int.TryParse(usuario.FindFirst("sub")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
        && id > 0 ? id : null;
}
