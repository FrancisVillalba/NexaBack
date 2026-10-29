using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;

namespace NexaBack.Infrastructure.Data.Context.Usuarios;

/// <summary>Unidad de trabajo para las tablas del esquema usuarios de NexaDB.</summary>
public sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("usuarios");
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
    }
}
