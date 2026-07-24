using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Data.Configurations.OrdenesCompra;

public class OrdenesCompraConfigurationTests
{
    private static DbContextOptions<DocFlowDbContext> CreateInMemoryOptions()
        => new DbContextOptionsBuilder<DocFlowDbContext>()
            .UseInMemoryDatabase($"OrdenesCompraConfigTest_{Guid.NewGuid()}")
            .Options;

    [Fact]
    public void OrdenCompra_ShouldMapToSnakeCaseTable()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(OrdenCompra));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ordenes_compra");
        entityType.FindProperty(nameof(OrdenCompra.Numero))!.GetColumnName().Should().Be("numero");
        entityType.FindProperty(nameof(OrdenCompra.ProveedorId))!.GetColumnName().Should().Be("proveedor_id");
        entityType.FindProperty(nameof(OrdenCompra.FormaPago))!.GetColumnName().Should().Be("forma_pago");
        entityType.FindProperty(nameof(OrdenCompra.ComentarioAprobacion))!.GetColumnName().Should().Be("comentario_aprobacion");
        entityType.FindProperty(nameof(OrdenCompra.MotivoAnulacion))!.GetColumnName().Should().Be("motivo_anulacion");
    }

    [Fact]
    public void OrdenCompra_ShouldMapCodigoMercadoPublico_AsNullableWithMaxLength40()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var property = context.Model.FindEntityType(typeof(OrdenCompra))!
            .FindProperty(nameof(OrdenCompra.CodigoMercadoPublico))!;

        property.GetColumnName().Should().Be("codigo_mercado_publico");
        property.GetMaxLength().Should().Be(40);
        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void OrdenCompra_ShouldUseXminAsOptimisticConcurrencyToken()
    {
        // Sin token de concurrencia, dos transiciones simultáneas hacen last-writer-wins
        // (p. ej. anular vs aprobar → la orden anulada queda aprobada sin error).
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var xmin = context.Model.FindEntityType(typeof(OrdenCompra))!.FindProperty("xmin");

        xmin.Should().NotBeNull();
        xmin!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void OrdenCompra_ShouldStoreEstadoAsString()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var estado = context.Model.FindEntityType(typeof(OrdenCompra))!
            .FindProperty(nameof(OrdenCompra.Estado))!;

        estado.GetColumnName().Should().Be("estado");
        estado.GetProviderClrType().Should().Be(typeof(string));
    }

    [Fact]
    public void OrdenCompra_ShouldHaveFilteredUniqueIndexOnNumero()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var index = context.Model.FindEntityType(typeof(OrdenCompra))!
            .GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(OrdenCompra.Numero)));

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Contain("numero IS NOT NULL");
    }

    [Fact]
    public void OrdenCompra_ShouldRestrictDeleteFromProveedor()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var foreignKey = context.Model.FindEntityType(typeof(OrdenCompra))!
            .GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Proveedor));

        foreignKey.Properties.Should().ContainSingle(p => p.Name == nameof(OrdenCompra.ProveedorId));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void OrdenCompraItem_ShouldMapAndCascadeFromOrdenCompra()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(OrdenCompraItem));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ordenes_compra_items");
        entityType.FindProperty(nameof(OrdenCompraItem.NumeroLinea))!.GetColumnName().Should().Be("numero_linea");
        entityType.FindProperty(nameof(OrdenCompraItem.Descripcion))!.GetMaxLength().Should().Be(300);
        entityType.FindProperty(nameof(OrdenCompraItem.PrecioUnitario))!.GetColumnName().Should().Be("precio_unitario");
        entityType.FindProperty(nameof(OrdenCompraItem.TotalLinea))!.GetColumnName().Should().Be("total_linea");

        var foreignKey = entityType.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(OrdenCompra));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void OrdenCompraAdjunto_ShouldMapAndCascadeFromOrdenCompra()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(OrdenCompraAdjunto));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ordenes_compra_adjuntos");
        entityType.FindProperty(nameof(OrdenCompraAdjunto.NombreArchivo))!.GetMaxLength().Should().Be(255);
        entityType.FindProperty(nameof(OrdenCompraAdjunto.ContentType))!.GetMaxLength().Should().Be(100);
        entityType.FindProperty(nameof(OrdenCompraAdjunto.Contenido))!.GetColumnName().Should().Be("contenido");
        entityType.FindProperty(nameof(OrdenCompraAdjunto.SubidoPor))!.GetColumnName().Should().Be("subido_por");

        var foreignKey = entityType.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(OrdenCompra));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void OrdenCompra_ItemsAndAdjuntos_ShouldUseFieldAccessMode()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(OrdenCompra))!;

        entityType.FindNavigation(nameof(OrdenCompra.Items))!
            .GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        entityType.FindNavigation(nameof(OrdenCompra.Adjuntos))!
            .GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
    }
}
