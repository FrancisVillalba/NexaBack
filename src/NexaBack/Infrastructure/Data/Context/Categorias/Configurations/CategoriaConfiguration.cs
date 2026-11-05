using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Categorias.Entity;
namespace NexaBack.Infrastructure.Data.Context.Categorias.Configurations;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categorias", "catalogo", t => t.HasCheckConstraint("ck_categorias_nombre", "length(btrim(\"Nombre\")) > 0 AND \"Nombre\" = btrim(\"Nombre\")"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property<string>("NombreNormalizado").HasComputedColumnSql("lower(btrim(\"Nombre\"))", stored: true);
        b.HasIndex("NombreNormalizado").IsUnique();
        b.Property(x => x.Activo).HasDefaultValue(true);
    }
}
