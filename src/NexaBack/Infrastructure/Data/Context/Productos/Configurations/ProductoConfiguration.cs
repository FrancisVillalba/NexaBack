using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Productos.Entity;

namespace NexaBack.Infrastructure.Data.Context.Productos.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos", "catalogo", table =>
        {
            table.HasCheckConstraint("ck_productos_codigo", "length(btrim(\"Codigo\")) > 0 AND \"Codigo\" = upper(btrim(\"Codigo\"))");
            table.HasCheckConstraint("ck_productos_nombre", "length(btrim(\"Nombre\")) > 0 AND \"Nombre\" = btrim(\"Nombre\")");
            table.HasCheckConstraint("ck_productos_categoria", "length(btrim(\"Categoria\")) > 0 AND \"Categoria\" = btrim(\"Categoria\")");
            table.HasCheckConstraint("ck_productos_codigo_barras", "\"CodigoBarras\" IS NULL OR (length(btrim(\"CodigoBarras\")) > 0 AND \"CodigoBarras\" = btrim(\"CodigoBarras\"))");
            table.HasCheckConstraint("ck_productos_importes", "\"Costo\" >= 0 AND \"PrecioVenta\" > 0 AND \"StockMinimo\" >= 0");
            table.HasCheckConstraint("ck_productos_unidad", "\"UnidadMedida\" IN ('unidad', 'caja', 'metro', 'kilo', 'bolsa', 'par')");
            table.HasCheckConstraint("ck_productos_stock_minimo", "\"UnidadMedida\" IN ('metro', 'kilo') OR \"StockMinimo\" = trunc(\"StockMinimo\")");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(1000);
        builder.Property(x => x.CodigoBarras).HasMaxLength(80);
        builder.HasIndex(x => x.CodigoBarras).IsUnique();
        builder.Property(x => x.Marca).HasMaxLength(100);
        builder.Property(x => x.Categoria).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UnidadMedida).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Costo).HasPrecision(14, 2);
        builder.Property(x => x.PrecioVenta).HasPrecision(14, 2);
        builder.Property(x => x.StockMinimo).HasPrecision(14, 3);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
    }
}
