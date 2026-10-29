using NexaBack.Infrastructure.Data.Context.Usuarios;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexaBack.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Base de datos no disponible.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // No exponer credenciales ni detalles del servidor en la respuesta HTTP.
            return HealthCheckResult.Unhealthy("Base de datos no disponible.");
        }
    }
}
