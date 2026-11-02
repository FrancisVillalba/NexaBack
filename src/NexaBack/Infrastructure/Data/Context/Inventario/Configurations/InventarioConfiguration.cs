using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Inventario.Entity;

namespace NexaBack.Infrastructure.Data.Context.Inventario.Configurations;

public sealed class DepositoConfiguration : IEntityTypeConfiguration<Deposito>
{
    public void Configure(EntityTypeBuilder<Deposito> builder)
    {
        builder.ToTable("depositos", "inventario", t => t.HasCheckConstraint("ck_depositos_nombre", "length(btrim(\"Nombre\")) > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(300);
        builder.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class ExistenciaConfiguration : IEntityTypeConfiguration<Existencia>
{
    public void Configure(EntityTypeBuilder<Existencia> builder)
    {
        builder.ToTable("existencias", "inventario", t => t.HasCheckConstraint("ck_existencias_cantidad", "\"Cantidad\" BETWEEN 0 AND 99999999999.999"));
        builder.HasKey(x => new { x.ProductoId, x.DepositoId });
        builder.Property(x => x.Cantidad).HasPrecision(14, 3);
        builder.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Deposito).WithMany().HasForeignKey(x => x.DepositoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("movimientos", "inventario", t =>
        {
            t.HasCheckConstraint("ck_movimientos_tipo", "\"Tipo\" IN ('entrada', 'salida', 'ajuste', 'transferencia')");
            t.HasCheckConstraint("ck_movimientos_cantidad", "\"Cantidad\" BETWEEN 0 AND 99999999999.999 AND (\"Tipo\" = 'ajuste' OR \"Cantidad\" > 0)");
            t.HasCheckConstraint("ck_movimientos_saldos", "\"StockAnterior\" BETWEEN 0 AND 99999999999.999 AND \"StockPosterior\" BETWEEN 0 AND 99999999999.999 AND \"StockPosterior\" = \"StockAnterior\" + \"Variacion\"");
            t.HasCheckConstraint("ck_movimientos_motivo", "length(btrim(\"Motivo\")) > 0");
            t.HasCheckConstraint("ck_movimientos_operacion", "\"OperacionId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
            t.HasCheckConstraint("ck_movimientos_destino", "(\"Tipo\" = 'transferencia' AND \"DepositoDestinoId\" IS NOT NULL AND \"DepositoDestinoId\" <> \"DepositoId\" AND \"StockDestinoAnterior\" IS NOT NULL AND \"StockDestinoPosterior\" IS NOT NULL AND \"StockDestinoAnterior\" BETWEEN 0 AND 99999999999.999 AND \"StockDestinoPosterior\" BETWEEN 0 AND 99999999999.999 AND \"StockDestinoPosterior\" = \"StockDestinoAnterior\" + \"Cantidad\") OR (\"Tipo\" <> 'transferencia' AND \"DepositoDestinoId\" IS NULL AND \"StockDestinoAnterior\" IS NULL AND \"StockDestinoPosterior\" IS NULL)");
            t.HasCheckConstraint("ck_movimientos_variacion", "(\"Tipo\" = 'entrada' AND \"Variacion\" = \"Cantidad\") OR (\"Tipo\" IN ('salida', 'transferencia') AND \"Variacion\" = -\"Cantidad\") OR (\"Tipo\" = 'ajuste' AND \"StockPosterior\" = \"Cantidad\" AND \"Variacion\" <> 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.HasIndex(x => x.OperacionId).IsUnique();
        builder.HasIndex(x => new { x.ProductoId, x.DepositoId, x.CreadoEn });
        builder.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Motivo).HasMaxLength(300).IsRequired();
        foreach (var nombre in new[] { "Cantidad", "Variacion", "StockAnterior", "StockPosterior", "StockDestinoAnterior", "StockDestinoPosterior" })
            builder.Property(nombre).HasPrecision(14, 3);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Deposito).WithMany().HasForeignKey(x => x.DepositoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DepositoDestino).WithMany().HasForeignKey(x => x.DepositoDestinoId).OnDelete(DeleteBehavior.Restrict);
    }
}
