using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;

public sealed class AccesoConfiguration : IEntityTypeConfiguration<Acceso>
{
    public void Configure(EntityTypeBuilder<Acceso> builder)
    {
        builder.ToTable("accesos", "usuarios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.Property(x => x.Modulo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Accion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class PerfilAccesoConfiguration : IEntityTypeConfiguration<PerfilAcceso>
{
    public void Configure(EntityTypeBuilder<PerfilAcceso> builder)
    {
        builder.ToTable("perfiles_accesos", "usuarios");
        builder.HasKey(x => new { x.PerfilId, x.AccesoId });
        builder.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Acceso).WithMany().HasForeignKey(x => x.AccesoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AccesoId);
    }
}
