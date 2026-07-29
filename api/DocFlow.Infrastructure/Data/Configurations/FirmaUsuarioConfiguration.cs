using DocFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlow.Infrastructure.Data.Configurations;

public class FirmaUsuarioConfiguration : IEntityTypeConfiguration<FirmaUsuario>
{
    public void Configure(EntityTypeBuilder<FirmaUsuario> builder)
    {
        builder.ToTable("firmas_usuario");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(f => f.ImagenFirma).HasColumnName("imagen_firma").HasColumnType("bytea").IsRequired();
        builder.Property(f => f.ContentType).HasColumnName("content_type")
               .HasMaxLength(FirmaUsuario.ContentTypeMaxLength).IsRequired();
        builder.Property(f => f.ClaveCifrada).HasColumnName("clave_cifrada");
        builder.Property(f => f.Sigla).HasColumnName("sigla").HasMaxLength(FirmaUsuario.SiglaMaxLength);
        builder.Property(f => f.CreadoEn).HasColumnName("creado_en");
        builder.Property(f => f.ActualizadoEn).HasColumnName("actualizado_en");

        // One signature per user.
        builder.HasIndex(f => f.UsuarioId).IsUnique();

        // FK to the user; deleting the user removes their signature.
        builder.HasOne<SeUsuari>()
               .WithMany()
               .HasForeignKey(f => f.UsuarioId)
               .HasPrincipalKey(u => u.UsuarioId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
