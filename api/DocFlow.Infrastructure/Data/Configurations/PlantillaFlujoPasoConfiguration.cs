using DocFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlow.Infrastructure.Data.Configurations;

public class PlantillaFlujoPasoConfiguration : IEntityTypeConfiguration<PlantillaFlujoPaso>
{
    public void Configure(EntityTypeBuilder<PlantillaFlujoPaso> builder)
    {
        builder.ToTable("plantilla_flujo_pasos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.CodForm)
               .HasColumnName("cod_form")
               .IsRequired();

        builder.Property(p => p.Orden).HasColumnName("orden").IsRequired();

        builder.Property(p => p.TipoAccion)
               .HasColumnName("tipo_accion")
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(p => p.ResponsableTipo)
               .HasColumnName("responsable_tipo")
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(p => p.ResponsableId).HasColumnName("responsable_id").IsRequired();
        builder.Property(p => p.Obligatorio).HasColumnName("obligatorio").IsRequired();

        builder.HasIndex(p => p.CodForm);

        // A template's workflow cannot have two steps sharing the same position.
        builder.HasIndex(p => new { p.CodForm, p.Orden }).IsUnique();
    }
}
