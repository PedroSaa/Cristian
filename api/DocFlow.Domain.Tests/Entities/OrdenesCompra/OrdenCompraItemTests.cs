using DocFlow.Domain.Entities.OrdenesCompra;
using FluentAssertions;
using Xunit;

namespace DocFlow.Domain.Tests.Entities.OrdenesCompra;

public class OrdenCompraItemTests
{
    private static readonly Guid OrdenCompraId = Guid.NewGuid();

    [Fact]
    public void Crear_ShouldSetAllProperties_AndComputeTotalLinea()
    {
        var id = Guid.NewGuid();

        var item = OrdenCompraItem.Crear(id, OrdenCompraId, 1, "Notebook 14 pulgadas", 2.5m, 400000m);

        item.Id.Should().Be(id);
        item.OrdenCompraId.Should().Be(OrdenCompraId);
        item.NumeroLinea.Should().Be(1);
        item.Descripcion.Should().Be("Notebook 14 pulgadas");
        item.Cantidad.Should().Be(2.5m);
        item.PrecioUnitario.Should().Be(400000m);
        item.TotalLinea.Should().Be(1000000m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ShouldThrow_WhenDescripcionMissing(string? descripcion)
    {
        var act = () => OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, descripcion!, 1m, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenDescripcionTooLong()
    {
        var descripcion = new string('x', 301);

        var act = () => OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, descripcion, 1m, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldAllowDescripcionAtMaxLength()
    {
        var descripcion = new string('x', 300);

        var item = OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, descripcion, 1m, 100m);

        item.Descripcion.Should().HaveLength(300);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crear_ShouldThrow_WhenCantidadNotPositive(decimal cantidad)
    {
        var act = () => OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, "Item", cantidad, 100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenPrecioUnitarioNegative()
    {
        var act = () => OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, "Item", 1m, -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Crear_ShouldAllowZeroPrecioUnitario()
    {
        var item = OrdenCompraItem.Crear(Guid.NewGuid(), OrdenCompraId, 1, "Muestra gratis", 1m, 0m);

        item.PrecioUnitario.Should().Be(0m);
        item.TotalLinea.Should().Be(0m);
    }
}
