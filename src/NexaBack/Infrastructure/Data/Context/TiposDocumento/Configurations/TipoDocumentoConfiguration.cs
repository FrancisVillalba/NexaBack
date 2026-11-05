using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.TiposDocumento.Entity;
namespace NexaBack.Infrastructure.Data.Context.TiposDocumento.Configurations;

public sealed class TipoDocumentoConfiguration : IEntityTypeConfiguration<TipoDocumento>
{
    public void Configure(EntityTypeBuilder<TipoDocumento> b)
    {
        b.ToTable("tipos_documento", "catalogo", t =>
        {
            t.HasCheckConstraint("ck_tipos_documento_codigo", "length(btrim(\"Codigo\")) > 0 AND \"Codigo\" = upper(btrim(\"Codigo\"))");
            t.HasCheckConstraint("ck_tipos_documento_nombre", "length(btrim(\"Nombre\")) > 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.Property(x => x.Codigo).HasMaxLength(20); b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Nombre).HasMaxLength(100);
        b.Property(x => x.Activo).HasDefaultValue(true);
        b.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
    }
}
