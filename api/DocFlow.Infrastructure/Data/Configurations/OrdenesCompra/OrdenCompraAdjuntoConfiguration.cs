using DocFlow.Domain.Entities.OrdenesCompra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlow.Infrastructure.Data.Configurations.OrdenesCompra;

public class OrdenCompraAdjuntoConfiguration : IEntityTypeConfiguration<OrdenCompraAdjunto>
{
    public void Configure(EntityTypeBuilder<OrdenCompraAdjunto> builder)
    {
        builder.ToTable("ordenes_compra_adjuntos");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.OrdenCompraId).HasColumnName("orden_compra_id");
        builder.Property(a => a.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Contenido).HasColumnName("contenido").IsRequired();
        builder.Property(a => a.Tamano).HasColumnName("tamano");
        builder.Property(a => a.SubidoPor).HasColumnName("subido_por");
        builder.Property(a => a.CreadoEn).HasColumnName("creado_en");

        builder.HasIndex(a => a.OrdenCompraId);
    }
}
