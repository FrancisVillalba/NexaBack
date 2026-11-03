using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Comercial.Entity;

namespace NexaBack.Infrastructure.Data.Context.Comercial.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes", "comercial", t => {
            t.HasCheckConstraint("ck_clientes_nombre", "length(btrim(\"NombreRazonSocial\")) > 0");
            t.HasCheckConstraint("ck_clientes_tipo_documento", "\"TipoDocumento\" IS NULL OR \"TipoDocumento\" IN ('CI', 'RUC', 'PASAPORTE', 'OTRO')");
            t.HasCheckConstraint("ck_clientes_documento", "length(btrim(\"Documento\")) > 0");
            t.HasCheckConstraint("ck_clientes_plazo", "\"PlazoPagoDias\" BETWEEN 0 AND 365");
            t.HasCheckConstraint("ck_clientes_credito", "\"LimiteCredito\" BETWEEN 0 AND 999999999999.99");
        });
        b.HasKey(x => x.Id);
        // Se conserva la secuencia existente para mantener IDs y relaciones de compras.
        b.Property(x => x.Id).HasDefaultValueSql("nextval('comercial.registros_comerciales_id_seq')");
        b.Property(x => x.NombreRazonSocial).HasMaxLength(300).IsRequired();
        b.Property(x => x.Documento).HasMaxLength(30).IsRequired();
        // La combinación de tipo y número es única dentro de este módulo.
        b.Property(x => x.TipoDocumento).HasMaxLength(20);
        b.HasIndex(x => new { x.TipoDocumento, x.Documento }).IsUnique();
        b.Property(x => x.Correo).HasMaxLength(254);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.Direccion).HasMaxLength(300);
        b.Property(x => x.Observaciones).HasMaxLength(500);
        b.Property(x => x.LimiteCredito).HasPrecision(14, 2);
        b.Property(x => x.Activo).HasDefaultValue(true);
        b.Property(x => x.Version).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
    }
}
