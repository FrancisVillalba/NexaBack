using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.UnidadesMedida.Entity;
namespace NexaBack.Infrastructure.Data.Context.UnidadesMedida.Configurations;

public sealed class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> b)
    {
        b.ToTable("unidades_medida", "catalogo", t => t.HasCheckConstraint("ck_unidades_medida_nombre", "length(btrim(\"Nombre\")) > 0 AND \"Nombre\" = btrim(\"Nombre\")"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property<string>("NombreNormalizado").HasComputedColumnSql("lower(btrim(\"Nombre\"))", stored: true);
        b.HasIndex("NombreNormalizado").IsUnique();
        b.Property(x => x.Activo).HasDefaultValue(true);
        // Los códigos conservan las reglas y las unidades históricas de compras y ventas.
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.ToTable("unidades_medida", "catalogo", t => t.HasCheckConstraint("ck_unidades_medida_codigo", "\"Codigo\" IN ('unidad', 'caja', 'metro', 'kilo', 'bolsa', 'par')"));

    }
}
