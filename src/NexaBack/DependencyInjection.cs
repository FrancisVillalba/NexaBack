using NexaBack.Repository.Usuarios.Perfiles;
using NexaBack.Repository.Usuarios.Usuarios;
using NexaBack.Repository.Usuarios.Personas;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.AspNetCore.Identity;
using NexaBack.Services.Usuarios.Personas;
using NexaBack.Services.Usuarios.Perfiles;
using NexaBack.Services.Usuarios.Usuarios;

namespace NexaBack;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Scoped: todos los repositorios de una solicitud comparten el mismo DbContext.
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPersonaService, PersonaService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        return services;
    }
}
