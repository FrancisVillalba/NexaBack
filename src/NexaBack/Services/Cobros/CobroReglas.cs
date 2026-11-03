using NexaBack.Common;
namespace NexaBack.Services.Cobros;

public static class CobroReglas
{
    public static void ValidarImporte(decimal importe)
    {
        if (importe <= 0 || importe > 999999999999.99m || decimal.Round(importe, 2) != importe)
            throw new BusinessException("El importe debe ser positivo, con hasta 12 enteros y 2 decimales.");
    }
    public static decimal Aplicar(string estadoVenta, decimal total, decimal cobrado, decimal importe)
    {
        ValidarImporte(importe);
        if (estadoVenta != "Confirmada") throw new BusinessException("Solo se cobran ventas confirmadas.", 409);
        if (cobrado < 0 || cobrado > total || importe > total - cobrado) throw new BusinessException("El cobro supera el saldo pendiente. Actualizá el detalle.", 409);
        return total - cobrado - importe;
    }
    public static void PermitirAnularVenta(bool tieneCobros)
    {
        if (tieneCobros) throw new BusinessException("La venta tiene cobros vigentes. Anulalos antes de anular la venta.", 409);
    }
    public static decimal Saldo(string estadoVenta, decimal total, decimal cobrado) => estadoVenta == "Anulada" ? 0 : total - cobrado;
    public static string Estado(string estadoVenta, decimal total, decimal cobrado)
        => estadoVenta == "Anulada" ? "No corresponde" : cobrado == 0 ? "Sin cobrar" : cobrado >= total ? "Saldada" : "Parcial";
}
