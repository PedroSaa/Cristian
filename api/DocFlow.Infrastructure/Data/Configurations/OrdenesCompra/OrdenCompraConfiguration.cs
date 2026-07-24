using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocFlow.Infrastructure.Data.Configurations.OrdenesCompra;

public class OrdenCompraConfiguration : IEntityTypeConfiguration<OrdenCompra>
{
    public void Configure(EntityTypeBuilder<OrdenCompra> builder)
    {
        builder.ToTable("ordenes_compra");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.Numero).HasColumnName("numero").HasMaxLength(50);
        builder.Property(o => o.ProveedorId).HasColumnName("proveedor_id");
        builder.Property(o => o.Fecha).HasColumnName("fecha");
        builder.Property(o => o.Moneda).HasColumnName("moneda").HasMaxLength(10).IsRequired();
        builder.Property(o => o.FormaPago).HasColumnName("forma_pago").HasMaxLength(200);
        builder.Property(o => o.PlazoEntrega).HasColumnName("plazo_entrega").HasMaxLength(200);
        builder.Property(o => o.LugarEntrega).HasColumnName("lugar_entrega").HasMaxLength(300);
        builder.Property(o => o.Observaciones).HasColumnName("observaciones").HasMaxLength(2000);
        builder.Property(o => o.Neto).HasColumnName("neto").HasPrecision(18, 2);
        builder.Property(o => o.Iva).HasColumnName("iva").HasPrecision(18, 2);
        builder.Property(o => o.Total).HasColumnName("total").HasPrecision(18, 2);

        builder.Property(o => o.Estado)
               .HasColumnName("estado")
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(o => o.CreadoPor).HasColumnName("creado_por");
        builder.Property(o => o.CreadoEn).HasColumnName("creado_en");
        builder.Property(o => o.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(o => o.AprobadoPor).HasColumnName("aprobado_por");
        builder.Property(o => o.AprobadoEn).HasColumnName("aprobado_en");
        builder.Property(o => o.ComentarioAprobacion).HasColumnName("comentario_aprobacion").HasMaxLength(1000);
        builder.Property(o => o.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(1000);
        builder.Property(o => o.CodigoMercadoPublico).HasColumnName("codigo_mercado_publico").HasMaxLength(40);

        // Optimistic concurrency via the Postgres system column xmin: without it,
        // concurrent state transitions are last-writer-wins (e.g. anular vs aprobar).
        // Conflicts surface as DbUpdateConcurrencyException → mapped to 409 at the API.
        builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .IsRowVersion();

        // Read-only FK to the Proveedores module — no navigation, deletion restricted.
        builder.HasOne<Proveedor>()
               .WithMany()
               .HasForeignKey(o => o.ProveedorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
               .WithOne()
               .HasForeignKey(i => i.OrdenCompraId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Adjuntos)
               .WithOne()
               .HasForeignKey(a => a.OrdenCompraId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(o => o.Adjuntos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // The business number is unique once assigned; drafts (numero IS NULL) are excluded.
        builder.HasIndex(o => o.Numero)
               .IsUnique()
               .HasFilter("numero IS NOT NULL");

        builder.HasIndex(o => o.Estado);
        builder.HasIndex(o => o.ProveedorId);
    }
}
