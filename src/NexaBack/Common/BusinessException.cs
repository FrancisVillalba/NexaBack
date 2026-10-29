namespace NexaBack.Common;

/// <summary>Error esperado que el controlador deberá traducir a una respuesta HTTP.</summary>
public sealed class BusinessException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
