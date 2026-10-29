using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;

public sealed class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("perfiles", "usuarios", table =>
            table.HasCheckConstraint("ck_perfiles_nombre", "length(btrim(\"Nombre\")) > 0 AND \"Nombre\" = btrim(\"Nombre\")"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.Descripcion).HasMaxLength(250);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}


