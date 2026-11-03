using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Ventas.Entity;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;
namespace NexaBack.Infrastructure.Data.Context.Ventas.Configurations;

public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.ToTable("ventas", "comercial", t => {
            t.HasCheckConstraint("ck_ventas_estado", "\"Estado\" IN ('Confirmada','Anulada')");
            t.HasCheckConstraint("ck_ventas_importes", "\"Subtotal\" BETWEEN 0 AND 999999999999.99 AND \"Descuento\" >= 0 AND \"Descuento\" < \"Subtotal\" AND \"Total\" = \"Subtotal\" - \"Descuento\"");
            t.HasCheckConstraint("ck_ventas_anulacion", "(\"Estado\" = 'Anulada' AND \"AnuladaEn\" IS NOT NULL AND \"MotivoAnulacion\" IS NOT NULL AND length(btrim(\"MotivoAnulacion\")) > 0) OR (\"Estado\" = 'Confirmada' AND \"AnuladaEn\" IS NULL AND \"MotivoAnulacion\" IS NULL)");
            t.HasCheckConstraint("ck_ventas_operacion", "\"OperacionId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => x.OperacionId).IsUnique();
        b.HasIndex(x => new { x.Estado, x.CreadoEn });
        b.Property(x => x.SolicitudHash).HasMaxLength(64);
        b.Property(x => x.ClienteNombre).HasMaxLength(300);
        b.Property(x => x.ClienteTipoDocumento).HasMaxLength(20);
        b.Property(x => x.ClienteDocumento).HasMaxLength(30);
        b.Property(x => x.DepositoNombre).HasMaxLength(100);
        b.Property(x => x.Estado).HasMaxLength(20);
        b.Property(x => x.Observaciones).HasMaxLength(500);
        b.Property(x => x.MotivoAnulacion).HasMaxLength(300);
        foreach (var campo in new[] { "Subtotal", "Descuento", "Total" }) b.Property(campo).HasPrecision(14, 2);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Deposito>().WithMany().HasForeignKey(x => x.DepositoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Detalles).WithOne(x => x.Venta).HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> b)
    {
        b.ToTable("ventas_detalles", "comercial", t => {
            t.HasCheckConstraint("ck_ventas_detalles_cantidad", "\"Cantidad\" > 0 AND \"Cantidad\" <= 99999999999.999");
            t.HasCheckConstraint("ck_ventas_detalles_precio", "\"PrecioUnitario\" > 0 AND \"PrecioUnitario\" <= 999999999999.99 AND \"CostoUnitario\" BETWEEN 0 AND 999999999999.99");
            t.HasCheckConstraint("ck_ventas_detalles_subtotal", "\"Subtotal\" BETWEEN 0 AND 999999999999.99 AND \"Subtotal\" = round(\"Cantidad\" * \"PrecioUnitario\", 2)");
            t.HasCheckConstraint("ck_ventas_detalles_unidad", "\"UnidadMedida\" IN ('metro','kilo') OR \"Cantidad\" = trunc(\"Cantidad\")");
            t.HasCheckConstraint("ck_ventas_detalles_movimientos", "\"MovimientoAnulacionId\" IS NULL OR \"MovimientoAnulacionId\" <> \"MovimientoSalidaId\"");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => new { x.VentaId, x.ProductoId }).IsUnique();
        b.HasIndex(x => x.MovimientoSalidaId).IsUnique();
        b.HasIndex(x => x.MovimientoAnulacionId).IsUnique();
        b.Property(x => x.ProductoCodigo).HasMaxLength(50);
        b.Property(x => x.ProductoNombre).HasMaxLength(150);
        b.Property(x => x.UnidadMedida).HasMaxLength(20);
        b.Property(x => x.Cantidad).HasPrecision(14, 3);
        foreach (var campo in new[] { "PrecioUnitario", "CostoUnitario", "Subtotal" }) b.Property(campo).HasPrecision(14, 2);
        b.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoSalida).WithMany().HasForeignKey(x => x.MovimientoSalidaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoAnulacion).WithMany().HasForeignKey(x => x.MovimientoAnulacionId).OnDelete(DeleteBehavior.Restrict);
    }
}
