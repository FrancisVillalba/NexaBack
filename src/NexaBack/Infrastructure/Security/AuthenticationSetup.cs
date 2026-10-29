using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexaBack.Repository.Usuarios.Usuarios;

namespace NexaBack.Infrastructure.Security;

public static class AuthenticationSetup
{
    public static IServiceCollection AddNexaAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(JwtOptions.HasValidKey, "Jwt:SigningKey debe ser una clave Base64 de al menos 32 bytes, configurada fuera del código.")
            .ValidateOnStart();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, settings) =>
            {
                var jwt = settings.Value;
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "nombreUsuario",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!int.TryParse(principal.FindFirst("sub")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                        {
                            context.Fail("Sesión inválida.");
                            return;
                        }
                        var repository = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                        var user = await repository.ObtenerConRelacionesAsync(id, context.HttpContext.RequestAborted);
                        // Una baja o cambio de perfil invalida el acceso aun antes del vencimiento del token.
                        if (user is null || !user.Activo || !user.Persona.Activo || !user.Perfil.Activo
                            || principal.FindFirst("perfilId")?.Value != user.PerfilId.ToString(CultureInfo.InvariantCulture)
                            || principal.FindFirst("role")?.Value != user.Perfil.Nombre
                            || principal.FindFirst("nombreUsuario")?.Value != user.NombreUsuario)
                            context.Fail("Sesión inválida.");
                    }
                };
            });
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Demasiados intentos", detail: "Esperá un minuto antes de volver a intentar.")
                    .ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }
}
