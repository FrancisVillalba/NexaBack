using System.Net.Mail;

namespace NexaBack.Common;

internal static class Validation
{
    public static string Required(string? value, string field, int maxLength)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result) || result.Length > maxLength)
            throw new BusinessException($"{field} es obligatorio y admite hasta {maxLength} caracteres.");
        return result;
    }

    public static string? Optional(string? value, string field, int maxLength)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result)) return null;
        if (result.Length > maxLength)
            throw new BusinessException($"{field} admite hasta {maxLength} caracteres.");
        return result;
    }

    public static string? Email(string? value)
    {
        var result = Optional(value, "Correo", 254);
        if (result is not null && !MailAddress.TryCreate(result, out _))
            throw new BusinessException("El correo no es válido.");
        return result;
    }

    public static int PageSize(int value)
    {
        if (value is < 1 or > 100) throw new BusinessException("El tamaño de página debe estar entre 1 y 100.");
        return value;
    }

    public static int Offset(int page, int size)
    {
        if (page < 1 || (long)(page - 1) * size > int.MaxValue)
            throw new BusinessException("El número de página no es válido.");
        return (page - 1) * size;
    }
}
