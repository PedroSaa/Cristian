using DocFlow.Application.OrdenesCompra.Queries.GetAdjuntoContenido;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompra;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Queries;

public class GetOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly GetOrdenCompraHandler _handler;

    public GetOrdenCompraHandlerTests()
    {
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _handler = new GetOrdenCompraHandler(_repoMock.Object, _proveedoresMock.Object);
    }

    [Fact]
    public async Task Should_Return_Dto_With_Items_Proveedor_And_Adjuntos_Metadata()
    {
        var oc = OrdenCompraTestFactory.Pendiente();
        var adjuntoMeta = new OrdenCompraAdjuntoMetadata(
            Guid.NewGuid(), "cotizacion.pdf", "application/pdf", 1234, OrdenCompraTestFactory.CreadorId, DateTime.UtcNow);
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(oc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([adjuntoMeta]);

        var result = await _handler.Handle(new GetOrdenCompraQuery(oc.Id), CancellationToken.None);

        result.Numero.Should().Be("OC-2026-0001");
        result.ProveedorNombre.Should().Be("Acme SA");
        result.ProveedorRut.Should().Be("12345678-5");
        result.Items.Should().HaveCount(2);
        result.Adjuntos.Should().ContainSingle(a => a.NombreArchivo == "cotizacion.pdf" && a.Tamano == 1234);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Missing()
    {
        var act = async () => await _handler.Handle(new GetOrdenCompraQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class GetAdjuntoContenidoHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly GetAdjuntoContenidoHandler _handler;

    public GetAdjuntoContenidoHandlerTests()
    {
        _handler = new GetAdjuntoContenidoHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_Contenido()
    {
        var ordenId = Guid.NewGuid();
        var adjunto = OrdenCompraAdjunto.Crear(
            Guid.NewGuid(), ordenId, "a.pdf", "application/pdf", [9, 8, 7], Guid.NewGuid());
        _repoMock.Setup(r => r.GetAdjuntoAsync(ordenId, adjunto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjunto);

        var result = await _handler.Handle(
            new GetAdjuntoContenidoQuery(ordenId, adjunto.Id), CancellationToken.None);

        result.NombreArchivo.Should().Be("a.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Contenido.Should().BeEquivalentTo(new byte[] { 9, 8, 7 });
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Missing()
    {
        var act = async () => await _handler.Handle(
            new GetAdjuntoContenidoQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
