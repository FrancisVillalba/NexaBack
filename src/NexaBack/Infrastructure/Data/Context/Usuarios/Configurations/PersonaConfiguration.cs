using NexaBack.Infrastructure.Data.Context.Usuarios.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NexaBack.Infrastructure.Data.Context.Usuarios.Configurations;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("personas", "usuarios", table =>
        {
            table.HasCheckConstraint("ck_personas_nombres", "length(btrim(\"Nombres\")) > 0");
            table.HasCheckConstraint("ck_personas_apellidos", "length(btrim(\"Apellidos\")) > 0");
            table.HasCheckConstraint("ck_personas_documento", "\"Documento\" IS NULL OR length(btrim(\"Documento\")) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Documento).HasMaxLength(30);
        builder.HasIndex(x => x.Documento).IsUnique();
        builder.Property(x => x.Correo).HasMaxLength(254);
        builder.Property(x => x.Telefono).HasMaxLength(30);
        builder.Property(x => x.Direccion).HasMaxLength(300);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}


