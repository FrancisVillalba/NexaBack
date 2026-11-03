using NexaBack.Common;
using NexaBack.Services.Inventario;

namespace NexaBack.Services.Compras;

public static class CompraReglas
{
    public const decimal MaximoImporte = 999999999999.99m;
    // El redondeo coincide con round(numeric, 2) de PostgreSQL.
    public static decimal Subtotal(decimal cantidad, decimal costo, string unidad)
    {
        InventarioReglas.Calcular("entrada", cantidad, 0, null, unidad);
        if (costo < 0 || costo > MaximoImporte || decimal.Round(costo, 2) != costo)
            throw new BusinessException("El costo admite hasta 12 enteros y 2 decimales y no puede ser negativo.");
        var subtotal = decimal.Round(cantidad * costo, 2, MidpointRounding.AwayFromZero);
        if (subtotal > MaximoImporte) throw new BusinessException("El importe de la línea supera el máximo permitido.");
        return subtotal;
    }
    public static void ValidarVersion(Guid actual, Guid recibida)
    {
        if (recibida == Guid.Empty || actual != recibida) throw new BusinessException("La compra cambió. Actualizá su detalle antes de continuar.", 409);
    }
}
