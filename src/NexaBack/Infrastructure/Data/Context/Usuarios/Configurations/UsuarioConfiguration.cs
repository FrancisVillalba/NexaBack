using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", "usuarios", table =>
        {
            table.HasCheckConstraint("ck_usuarios_nombre", "length(btrim(\"NombreUsuario\")) > 0 AND \"NombreUsuario\" = lower(btrim(\"NombreUsuario\"))");
            table.HasCheckConstraint("ck_usuarios_password_hash", "length(btrim(\"PasswordHash\")) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        // El servicio de alta debe normalizar a minúsculas y quitar espacios extremos.
        builder.Property(x => x.NombreUsuario).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.NombreUsuario).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasOne(x => x.Persona).WithOne(x => x.Usuario)
            .HasForeignKey<Usuario>(x => x.PersonaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Perfil).WithMany(x => x.Usuarios)
            .HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Restrict);
    }
}


