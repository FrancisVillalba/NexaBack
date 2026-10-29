using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NexaBack.Common;

public sealed class BusinessExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException businessException) return false;

        httpContext.Response.StatusCode = businessException.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = businessException.StatusCode,
                Title = businessException.StatusCode switch
                {
                    401 => "No autorizado",
                    404 => "Registro no encontrado",
                    409 => "Conflicto de datos",
                    _ => "Solicitud inválida"
                },
                Detail = businessException.Message
            }
        });
    }
}
