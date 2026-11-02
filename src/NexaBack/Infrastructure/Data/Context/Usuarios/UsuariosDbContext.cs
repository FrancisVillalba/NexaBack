using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;
using NexaBack.Infrastructure.Persistence;

namespace NexaBack.Infrastructure.Data.Context.Usuarios;

/// <summary>Unidad de trabajo para las tablas del esquema usuarios de NexaDB.</summary>
public sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options, IUsuarioAuditoria usuarioAuditoria) : DbContext(options)
{
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Acceso> Accesos => Set<Acceso>();
    public DbSet<PerfilAcceso> PerfilesAccesos => Set<PerfilAcceso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("usuarios");
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
        modelBuilder.ApplyConfiguration(new AccesoConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilAccesoConfiguration());
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(x => typeof(IEntidadAuditable).IsAssignableFrom(x.ClrType)).ToArray())
        {
            var builder = modelBuilder.Entity(entity.ClrType);
            builder.Property<DateTime>(nameof(IEntidadAuditable.CreadoEn)).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property<DateTime?>(nameof(IEntidadAuditable.ModificadoEn));
            builder.HasOne(typeof(Usuario)).WithMany().HasForeignKey(nameof(IEntidadAuditable.CreadoPor)).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(typeof(Usuario)).WithMany().HasForeignKey(nameof(IEntidadAuditable.ModificadoPor)).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public int? UsuarioAuditoriaId => usuarioAuditoria.UsuarioId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepararAuditoria();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepararAuditoria();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepararAuditoria()
    {
        var ahora = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IEntidadAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreadoEn = ahora;
                entry.Entity.CreadoPor = usuarioAuditoria.UsuarioId;
                entry.Entity.ModificadoEn = null;
                entry.Entity.ModificadoPor = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                // La creación es inmutable: una edición solo cambia los datos de modificación.
                entry.Property(x => x.CreadoEn).CurrentValue = entry.Property(x => x.CreadoEn).OriginalValue;
                entry.Property(x => x.CreadoEn).IsModified = false;
                entry.Property(x => x.CreadoPor).CurrentValue = entry.Property(x => x.CreadoPor).OriginalValue;
                entry.Property(x => x.CreadoPor).IsModified = false;
                entry.Entity.ModificadoEn = ahora;
                entry.Entity.ModificadoPor = usuarioAuditoria.UsuarioId;
            }
        }
    }
}
