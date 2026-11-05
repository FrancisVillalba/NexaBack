using NexaBack.Infrastructure.Data.Context.Presupuestos.Entity;
using NexaBack.Infrastructure.Data.Context.Presupuestos.Configurations;
using NexaBack.Infrastructure.Data.Context.Pagos.Entity;
using NexaBack.Infrastructure.Data.Context.Pagos.Configurations;
using NexaBack.Infrastructure.Data.Context.Cobros.Entity;
using NexaBack.Infrastructure.Data.Context.Cobros.Configurations;
using NexaBack.Infrastructure.Data.Context.Ventas.Entity;
using NexaBack.Infrastructure.Data.Context.Ventas.Configurations;
using NexaBack.Infrastructure.Data.Context.Compras.Entity;
using NexaBack.Infrastructure.Data.Context.Compras.Configurations;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;
using NexaBack.Infrastructure.Persistence;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Context.Productos.Configurations;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Configurations;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Configurations;

namespace NexaBack.Infrastructure.Data.Context.Usuarios;

/// <summary>Unidad de trabajo compartida: usuarios y catálogo usan la misma auditoría y transacción.</summary>
public sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options, IUsuarioAuditoria usuarioAuditoria) : DbContext(options)
{
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Acceso> Accesos => Set<Acceso>();
    public DbSet<PerfilAcceso> PerfilesAccesos => Set<PerfilAcceso>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Deposito> Depositos => Set<Deposito>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<PresupuestoDetalle> PresupuestosDetalles => Set<PresupuestoDetalle>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentasDetalles => Set<VentaDetalle>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalles => Set<CompraDetalle>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("usuarios");
        modelBuilder.ApplyConfiguration(new PresupuestoConfiguration());
        modelBuilder.ApplyConfiguration(new PresupuestoDetalleConfiguration());
        modelBuilder.ApplyConfiguration(new PagoConfiguration());
        modelBuilder.ApplyConfiguration(new CobroConfiguration());
        modelBuilder.ApplyConfiguration(new VentaConfiguration());
        modelBuilder.ApplyConfiguration(new VentaDetalleConfiguration());
        modelBuilder.ApplyConfiguration(new CompraConfiguration());
        modelBuilder.ApplyConfiguration(new CompraDetalleConfiguration());
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
        modelBuilder.ApplyConfiguration(new AccesoConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilAccesoConfiguration());
        modelBuilder.ApplyConfiguration(new ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new DepositoConfiguration());
        modelBuilder.ApplyConfiguration(new ExistenciaConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoInventarioConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new ProveedorConfiguration());
        modelBuilder.HasSequence<int>("registros_comerciales_id_seq", "comercial");
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(x => typeof(IEntidadAuditable).IsAssignableFrom(x.ClrType) && x.BaseType == null).ToArray())
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
