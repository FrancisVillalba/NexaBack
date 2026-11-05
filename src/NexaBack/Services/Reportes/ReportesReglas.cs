using NexaBack.Common;
using NexaBack.Infrastructure.Data.Dtos.Reportes;
namespace NexaBack.Services.Reportes;

public static class ReportesReglas
{
    public const int LimiteExportacion = 10000;
    public static readonly string[] Tipos = ["ventas", "compras", "cobros", "pagos", "inventario", "por-cobrar", "por-pagar"];
    public static bool UsaPeriodo(string tipo) => tipo is "ventas" or "compras" or "cobros" or "pagos";
    public static string Permiso(string tipo) => tipo switch
    {
        "ventas" or "cobros" => "ventas.ver",
        "compras" or "pagos" => "compras.ver",
        "inventario" => "inventario.ver",
        "por-cobrar" => "clientes.cuenta_corriente",
        "por-pagar" => "proveedores.cuenta_corriente",
        _ => throw new BusinessException("Tipo de reporte inválido.")
    };
    public static void Autorizar(string tipo, IReadOnlySet<string> permisos)
    {
        if (!permisos.Contains(Permiso(tipo))) throw new BusinessException("Tu perfil no tiene acceso a esta sección del reporte.", 403);
    }
    public static void Validar(ReporteConsulta q)
    {
        Permiso(q.Tipo);
        Deposito(q.DepositoId);
        Validation.Offset(q.Pagina, Validation.PageSize(q.TamanoPagina));
        if (UsaPeriodo(q.Tipo) && (q.Desde == default || q.Hasta == default || q.Desde > q.Hasta || q.Hasta == DateOnly.MaxValue))
            throw new BusinessException("Indicá un período válido, con Desde anterior o igual a Hasta.");
        if (UsaPeriodo(q.Tipo) && q.Hasta.DayNumber - q.Desde.DayNumber > 365)
            throw new BusinessException("Consultá períodos de hasta 366 días.");
    }
    public static void Exportacion(int registros)
    {
        if (registros > LimiteExportacion) throw new BusinessException("La exportación supera 10.000 filas. Reducí el período o seleccioná un depósito; no se exportan resultados incompletos.", 422);
    }
    public static void Deposito(int? id) { if (id.HasValue && id <= 0) throw new BusinessException("Depósito inválido."); }
    public static DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "America/Argentina/Buenos_Aires").DateTime);
    public static DateTime InicioUtc(DateOnly fecha) => TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"));
}
