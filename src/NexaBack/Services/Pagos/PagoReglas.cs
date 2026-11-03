using NexaBack.Common;
namespace NexaBack.Services.Pagos;

// La deuda nace al recibir la compra; pagar o anular un pago no mueve mercadería.
public static class PagoReglas
{
    public static void ValidarImporte(decimal importe)
    {
        if (importe <= 0 || importe > 999999999999.99m || decimal.Round(importe, 2) != importe)
            throw new BusinessException("El importe debe ser positivo, con hasta 12 enteros y 2 decimales.");
    }
    public static decimal Aplicar(string estadoCompra, decimal total, decimal pagado, decimal importe)
    {
        ValidarImporte(importe);
        if (estadoCompra != "Recibida") throw new BusinessException("Solo se pagan compras recibidas.", 409);
        if (pagado < 0 || pagado > total || importe > total - pagado)
            throw new BusinessException("El pago supera el saldo pendiente. Actualizá el detalle.", 409);
        return total - pagado - importe;
    }
    public static decimal Saldo(string estadoCompra, decimal total, decimal pagado) => estadoCompra == "Recibida" ? total - pagado : 0;
    public static string Estado(string estadoCompra, decimal total, decimal pagado)
        => estadoCompra != "Recibida" ? "No corresponde" : pagado == 0 ? "Sin pagar" : pagado >= total ? "Saldada" : "Parcial";
}
