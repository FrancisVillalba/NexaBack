using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Compras.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;

namespace NexaBack.Infrastructure.Data.Context.Compras.Configurations;

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> b)
    {
        b.ToTable("compras", "comercial", t => {
            t.HasCheckConstraint("ck_compras_operacion", "\"OperacionId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
            t.HasCheckConstraint("ck_compras_estado", "\"Estado\" IN ('Pendiente', 'Recibida', 'Cancelada')");
            t.HasCheckConstraint("ck_compras_total", "\"Total\" BETWEEN 0 AND 999999999999.99");
            t.HasCheckConstraint("ck_compras_recepcion", "(\"Estado\" = 'Recibida') = (\"RecibidoEn\" IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => x.OperacionId).IsUnique();
        b.HasIndex(x => new { x.Estado, x.CreadoEn });
        b.Property(x => x.SolicitudHash).HasMaxLength(64);
        b.Property(x => x.ProveedorNombre).HasMaxLength(301);
        b.Property(x => x.ProveedorTipoDocumento).HasMaxLength(20);
        b.Property(x => x.ProveedorDocumento).HasMaxLength(50);
        b.Property(x => x.DepositoNombre).HasMaxLength(100);
        b.Property(x => x.Estado).HasMaxLength(20);
        b.Property(x => x.Observaciones).HasMaxLength(500);
        b.Property(x => x.Total).HasPrecision(14, 2);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Deposito>().WithMany().HasForeignKey(x => x.DepositoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Detalles).WithOne(x => x.Compra).HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> b)
    {
        b.ToTable("compras_detalles", "comercial", t => {
            t.HasCheckConstraint("ck_compras_detalles_cantidad", "\"Cantidad\" > 0 AND \"Cantidad\" <= 99999999999.999");
            t.HasCheckConstraint("ck_compras_detalles_costo", "\"CostoUnitario\" BETWEEN 0 AND 999999999999.99");
            t.HasCheckConstraint("ck_compras_detalles_subtotal", "\"Subtotal\" BETWEEN 0 AND 999999999999.99 AND \"Subtotal\" = round(\"Cantidad\" * \"CostoUnitario\", 2)");
            t.HasCheckConstraint("ck_compras_detalles_unidad", "\"UnidadMedida\" IN ('metro', 'kilo') OR \"Cantidad\" = trunc(\"Cantidad\")");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => new { x.CompraId, x.ProductoId }).IsUnique();
        b.HasIndex(x => x.MovimientoId).IsUnique();
        b.Property(x => x.ProductoCodigo).HasMaxLength(50);
        b.Property(x => x.ProductoNombre).HasMaxLength(150);
        b.Property(x => x.UnidadMedida).HasMaxLength(20);
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.CostoUnitario).HasPrecision(14, 2);
        b.Property(x => x.Subtotal).HasPrecision(14, 2);
        b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Movimiento).WithMany().HasForeignKey(x => x.MovimientoId).OnDelete(DeleteBehavior.Restrict);
    }
}
