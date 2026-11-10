using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Presupuestos.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
namespace NexaBack.Infrastructure.Data.Context.Presupuestos.Configurations;

public sealed class PresupuestoConfiguration : IEntityTypeConfiguration<Presupuesto>
{
    public void Configure(EntityTypeBuilder<Presupuesto> b)
    {
        b.ToTable("presupuestos", "comercial", t =>
        {
            t.HasCheckConstraint("ck_presupuestos_estado", "\"Estado\" IN ('Borrador','Aprobado','Cancelado','Convertido')");
            t.HasCheckConstraint("ck_presupuestos_importes", "\"Subtotal\" BETWEEN 0 AND 999999999999.99 AND \"Descuento\" >= 0 AND \"Descuento\" < \"Subtotal\" AND \"Total\" = \"Subtotal\" - \"Descuento\"");
            t.HasCheckConstraint("ck_presupuestos_fecha", "\"FechaVencimiento\" > DATE '0001-01-01'");
            t.HasCheckConstraint("ck_presupuestos_operacion", "\"OperacionId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
            t.HasCheckConstraint("ck_presupuestos_venta", "(\"Estado\" = 'Convertido' AND \"VentaId\" IS NOT NULL) OR (\"Estado\" <> 'Convertido' AND \"VentaId\" IS NULL)");
            t.HasCheckConstraint("ck_presupuestos_cancelacion", "(\"Estado\" = 'Cancelado' AND \"CanceladoEn\" IS NOT NULL AND \"MotivoCancelacion\" IS NOT NULL AND length(btrim(\"MotivoCancelacion\")) > 0) OR (\"Estado\" <> 'Cancelado' AND \"CanceladoEn\" IS NULL AND \"MotivoCancelacion\" IS NULL)");
            t.HasCheckConstraint("ck_presupuestos_aprobacion", "(\"Estado\" IN ('Aprobado','Convertido') AND \"AprobadoEn\" IS NOT NULL AND \"AprobadoPor\" IS NOT NULL) OR \"Estado\" IN ('Borrador','Cancelado')");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => x.OperacionId).IsUnique();
        b.HasIndex(x => x.VentaId).IsUnique();
        b.HasIndex(x => new { x.Estado, x.CreadoEn });
        b.Property(x => x.SolicitudHash).HasMaxLength(64);
        b.Property(x => x.EdicionHash).HasMaxLength(64);
        b.Property(x => x.Estado).HasMaxLength(20);
        b.Property(x => x.Observaciones).HasMaxLength(500);
        b.Property(x => x.MotivoCancelacion).HasMaxLength(300);
        foreach (var campo in new[] { "Subtotal", "Descuento", "Total" }) b.Property(campo).HasPrecision(14, 2);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Deposito).WithMany().HasForeignKey(x => x.DepositoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AprobadoPor).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Detalles).WithOne(x => x.Presupuesto).HasForeignKey(x => x.PresupuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class PresupuestoDetalleConfiguration : IEntityTypeConfiguration<PresupuestoDetalle>
{
    public void Configure(EntityTypeBuilder<PresupuestoDetalle> b)
    {
        b.ToTable("presupuestos_detalles", "comercial", t =>
        {
            t.HasCheckConstraint("ck_presupuestos_detalles_cantidad", "\"Cantidad\" > 0 AND \"Cantidad\" <= 99999999999.999");
            t.HasCheckConstraint("ck_presupuestos_detalles_precio", "\"PrecioUnitario\" > 0 AND \"PrecioUnitario\" <= 999999999999.99");
            t.HasCheckConstraint("ck_presupuestos_detalles_subtotal", "\"Subtotal\" BETWEEN 0 AND 999999999999.99 AND \"Subtotal\" = round(\"Cantidad\" * \"PrecioUnitario\", 2)");
            t.HasCheckConstraint("ck_presupuestos_detalles_unidad", "\"UnidadMedida\" IN ('metro','kilo') OR \"Cantidad\" = trunc(\"Cantidad\")");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => new { x.PresupuestoId, x.ProductoId }).IsUnique();
        b.Property(x => x.UnidadMedida).HasMaxLength(20);
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        b.Property(x => x.PrecioUnitario).HasPrecision(14, 2);
        b.Property(x => x.Subtotal).HasPrecision(14, 2);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
