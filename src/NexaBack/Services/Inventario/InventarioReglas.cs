using NexaBack.Common;

namespace NexaBack.Services.Inventario;

// Reglas sin acceso a datos: permiten comprobar los cálculos sin tocar la base de datos.
public static class InventarioReglas
{
    public const decimal Maximo = 99999999999.999m;

    public static (decimal Origen, decimal? Destino) Calcular(string tipo, decimal cantidad, decimal actual, decimal? destino, string unidad)
    {
        if (tipo is not ("entrada" or "salida" or "ajuste" or "transferencia")) throw new BusinessException("Tipo de movimiento no válido.");
        if (cantidad < 0 || cantidad > Maximo || decimal.Round(cantidad, 3) != cantidad || (tipo != "ajuste" && cantidad == 0))
            throw new BusinessException("La cantidad admite hasta 11 enteros y 3 decimales; solo un ajuste permite cero.");
        if (unidad is not ("metro" or "kilo") && decimal.Truncate(cantidad) != cantidad)
            throw new BusinessException("Esta unidad de medida requiere cantidades enteras.");
        var nuevo = tipo switch { "entrada" => actual + cantidad, "ajuste" => cantidad, _ => actual - cantidad };
        if (nuevo < 0) throw new BusinessException("No hay existencias suficientes para realizar la operación.", 409);
        if (nuevo > Maximo) throw new BusinessException("El saldo supera el máximo permitido.");
        if (tipo == "ajuste" && nuevo == actual) throw new BusinessException("El stock ya coincide con la cantidad indicada.");
        decimal? nuevoDestino = null;
        if (tipo == "transferencia")
        {
            if (destino is null) throw new BusinessException("Seleccioná un depósito de destino.");
            nuevoDestino = destino.Value + cantidad;
            if (nuevoDestino > Maximo) throw new BusinessException("El saldo del destino supera el máximo permitido.");
        }
        return (nuevo, nuevoDestino);
    }
}
