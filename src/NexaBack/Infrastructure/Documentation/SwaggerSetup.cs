using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NexaBack.Infrastructure.Documentation;

public static class SwaggerSetup
{
    public static IServiceCollection AddNexaSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NEXA Backend API",
                Version = "v1",
                Description = "Primero ejecutá el login. Después copiá el token en Authorize para probar los endpoints protegidos."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Pegá solamente el token, sin escribir Bearer delante."
            });
            options.OperationFilter<JwtSecurityOperationFilter>();
        });
        return services;
    }
}

public sealed class JwtSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any()) return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];
        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente, inválido o vencido." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "El perfil no tiene permiso para esta operación." });
    }
}
