using NexaBack.Common;
namespace NexaBack.Services.Presupuestos;

public static class PresupuestoReglas
{
    // El vencimiento es inclusivo y sigue el calendario del negocio, no la zona horaria del servidor.
    public static DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "America/Argentina/Buenos_Aires").DateTime);
    public static bool Vencido(string estado, DateOnly vencimiento, DateOnly hoy)
        => estado is "Borrador" or "Aprobado" && vencimiento < hoy;
    public static void Fecha(DateOnly fecha, DateOnly hoy)
    {
        if (fecha == default || fecha < hoy) throw new BusinessException("La fecha de vencimiento debe ser hoy o posterior.");
    }
    public static void Version(Guid actual, Guid enviada)
    {
        if (enviada == Guid.Empty || enviada != actual) throw new BusinessException("El presupuesto cambió. Actualizá el detalle.", 409);
    }
    public static void Editar(string estado)
    {
        if (estado != "Borrador") throw new BusinessException("Solo se editan presupuestos en borrador.", 409);
    }
    public static void Aprobar(string estado, DateOnly vencimiento, DateOnly hoy)
    {
        Editar(estado);
        if (vencimiento < hoy) throw new BusinessException("El presupuesto venció. Editá su vencimiento antes de aprobarlo.", 409);
    }
    public static void Convertir(string estado, DateOnly vencimiento, DateOnly hoy)
    {
        if (estado != "Aprobado") throw new BusinessException("Solo se convierten presupuestos aprobados.", 409);
        if (vencimiento < hoy) throw new BusinessException("El presupuesto está vencido y no admite conversión.", 409);
    }
    public static void Cancelar(string estado)
    {
        if (estado is not ("Borrador" or "Aprobado")) throw new BusinessException("Solo se cancelan presupuestos en borrador o aprobados.", 409);
    }
}
