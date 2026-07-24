using DocFlow.Domain.Entities.OrdenesCompra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlow.Infrastructure.Data.Configurations.OrdenesCompra;

public class OrdenCompraItemConfiguration : IEntityTypeConfiguration<OrdenCompraItem>
{
    public void Configure(EntityTypeBuilder<OrdenCompraItem> builder)
    {
        builder.ToTable("ordenes_compra_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.OrdenCompraId).HasColumnName("orden_compra_id");
        builder.Property(i => i.NumeroLinea).HasColumnName("numero_linea");
        builder.Property(i => i.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsRequired();
        builder.Property(i => i.Cantidad).HasColumnName("cantidad").HasPrecision(18, 4);
        builder.Property(i => i.PrecioUnitario).HasColumnName("precio_unitario").HasPrecision(18, 2);
        builder.Property(i => i.TotalLinea).HasColumnName("total_linea").HasPrecision(18, 2);

        builder.HasIndex(i => i.OrdenCompraId);
    }
}
