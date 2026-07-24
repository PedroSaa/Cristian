using DocFlow.Domain.Entities.OrdenesCompra;
using FluentAssertions;
using Xunit;

namespace DocFlow.Domain.Tests.Entities.OrdenesCompra;

public class OrdenCompraAdjuntoTests
{
    private static readonly Guid OrdenCompraId = Guid.NewGuid();
    private static readonly Guid SubidoPor = Guid.NewGuid();
    private static readonly byte[] Contenido = [1, 2, 3, 4, 5];

    [Fact]
    public void Crear_ShouldSetAllProperties_AndComputeTamano()
    {
        var id = Guid.NewGuid();

        var adjunto = OrdenCompraAdjunto.Crear(
            id, OrdenCompraId, "cotizacion.pdf", "application/pdf", Contenido, SubidoPor);

        adjunto.Id.Should().Be(id);
        adjunto.OrdenCompraId.Should().Be(OrdenCompraId);
        adjunto.NombreArchivo.Should().Be("cotizacion.pdf");
        adjunto.ContentType.Should().Be("application/pdf");
        adjunto.Contenido.Should().BeEquivalentTo(Contenido);
        adjunto.Tamano.Should().Be(Contenido.LongLength);
        adjunto.SubidoPor.Should().Be(SubidoPor);
        adjunto.CreadoEn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ShouldThrow_WhenNombreArchivoMissing(string? nombre)
    {
        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, nombre!, "application/pdf", Contenido, SubidoPor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenNombreArchivoTooLong()
    {
        var nombre = new string('a', 256);

        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, nombre, "application/pdf", Contenido, SubidoPor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenContentTypeTooLong()
    {
        var contentType = new string('a', 101);

        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, "archivo.pdf", contentType, Contenido, SubidoPor);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Crear_ShouldThrow_WhenContentTypeMissing(string? contentType)
    {
        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, "archivo.pdf", contentType!, Contenido, SubidoPor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenContenidoEmpty()
    {
        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, "archivo.pdf", "application/pdf", [], SubidoPor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenSubidoPorEmpty()
    {
        var act = () => OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), OrdenCompraId, "archivo.pdf", "application/pdf", Contenido, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
