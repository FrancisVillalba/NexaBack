using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Cobros.Entity;
namespace NexaBack.Infrastructure.Data.Context.Cobros.Configurations;

public sealed class CobroConfiguration : IEntityTypeConfiguration<Cobro>
{
    public void Configure(EntityTypeBuilder<Cobro> b)
    {
        b.ToTable("cobros", "comercial", t => {
            t.HasCheckConstraint("ck_cobros_importe", "\"Importe\" > 0 AND \"Importe\" <= 999999999999.99");
            t.HasCheckConstraint("ck_cobros_fecha", "\"FechaCobro\" > DATE '0001-01-01'");
            t.HasCheckConstraint("ck_cobros_estado", "\"Estado\" IN ('Vigente','Anulado')");
            t.HasCheckConstraint("ck_cobros_medio", "\"MedioPago\" IN ('EFECTIVO','TRANSFERENCIA','TARJETA','OTRO')");
            t.HasCheckConstraint("ck_cobros_operacion", "\"OperacionId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
            t.HasCheckConstraint("ck_cobros_anulacion", "(\"Estado\" = 'Anulado' AND \"AnuladoEn\" IS NOT NULL AND \"MotivoAnulacion\" IS NOT NULL AND length(btrim(\"MotivoAnulacion\")) > 0) OR (\"Estado\" = 'Vigente' AND \"AnuladoEn\" IS NULL AND \"MotivoAnulacion\" IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();
        b.HasIndex(x => x.OperacionId).IsUnique();
        b.HasIndex(x => new { x.VentaId, x.Estado });
        b.Property(x => x.SolicitudHash).HasMaxLength(64);
        b.Property(x => x.Importe).HasPrecision(14, 2);
        b.Property(x => x.MedioPago).HasMaxLength(20);
        b.Property(x => x.Referencia).HasMaxLength(100);
        b.Property(x => x.Observaciones).HasMaxLength(500);
        b.Property(x => x.Estado).HasMaxLength(20);
        b.Property(x => x.MotivoAnulacion).HasMaxLength(300);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
    }
}
