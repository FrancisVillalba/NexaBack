using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Marcas.Entity;
namespace NexaBack.Infrastructure.Data.Context.Marcas.Configurations;

public sealed class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> b)
    {
        b.ToTable("marcas", "catalogo", t => t.HasCheckConstraint("ck_marcas_nombre", "length(btrim(\"Nombre\")) > 0 AND \"Nombre\" = btrim(\"Nombre\")"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.Property(x => x.Descripcion).HasMaxLength(300);
        // La restricción de base también protege solicitudes simultáneas con distintas mayúsculas.
        b.Property<string>("NombreNormalizado").HasComputedColumnSql("lower(btrim(\"Nombre\"))", stored: true);
        b.HasIndex("NombreNormalizado").IsUnique();
        b.Property(x => x.Activo).HasDefaultValue(true);
        b.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
    }
}
