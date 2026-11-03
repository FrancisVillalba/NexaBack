using NexaBack.Common;
using NexaBack.Services.Inventario;
namespace NexaBack.Services.Ventas;

// Importes decimales y redondeo por línea, iguales en el servidor y PostgreSQL.
public static class VentaReglas
{
    public const decimal MaximoImporte = 999999999999.99m;
    public static void Cantidad(decimal cantidad, string unidad) => InventarioReglas.Calcular("entrada", cantidad, 0, null, unidad);
    public static decimal Subtotal(decimal cantidad, decimal precio, string unidad)
    {
        Cantidad(cantidad, unidad);
        if (precio <= 0 || precio > MaximoImporte || decimal.Round(precio, 2) != precio) throw new BusinessException("El precio de venta debe ser positivo y admitir hasta dos decimales.");
        var subtotal = decimal.Round(cantidad * precio, 2, MidpointRounding.AwayFromZero);
        if (subtotal > MaximoImporte) throw new BusinessException("El importe supera el máximo permitido.");
        return subtotal;
    }
    public static void Descuento(decimal descuento)
    {
        if (descuento < 0 || descuento > MaximoImporte || decimal.Round(descuento, 2) != descuento) throw new BusinessException("El descuento no puede ser negativo y admite hasta dos decimales.");
    }
    public static decimal Total(decimal subtotal, decimal descuento)
    {
        Descuento(descuento);
        if (subtotal <= 0 || subtotal > MaximoImporte || descuento >= subtotal) throw new BusinessException("El total debe ser positivo y el descuento menor al subtotal.");
        return subtotal - descuento;
    }
    public static void Version(Guid actual, Guid enviada)
    {
        if (enviada == Guid.Empty || actual != enviada) throw new BusinessException("La venta cambió. Actualizá su detalle antes de continuar.", 409);
    }
}
