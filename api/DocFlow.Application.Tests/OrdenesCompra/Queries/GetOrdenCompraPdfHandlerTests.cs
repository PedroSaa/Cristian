using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Application.OrdenesCompra.Queries.GetOrdenCompraPdf;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Queries;

public class GetOrdenCompraPdfHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<ISeUsuariRepository> _usuariosMock = new();
    private readonly Mock<IOrdenCompraPdfService> _pdfMock = new();
    private readonly GetOrdenCompraPdfHandler _handler;

    public GetOrdenCompraPdfHandlerTests()
    {
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _pdfMock.Setup(p => p.Generar(It.IsAny<OrdenCompraPdfData>()))
            .Returns([0x25, 0x50, 0x44, 0x46]); // "%PDF"

        _handler = new GetOrdenCompraPdfHandler(
            _repoMock.Object, _proveedoresMock.Object, _usuariosMock.Object, _pdfMock.Object);
    }

    [Fact]
    public async Task Should_Generate_Pdf_With_Order_Data()
    {
        var oc = OrdenCompraTestFactory.Pendiente();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(new GetOrdenCompraPdfQuery(oc.Id), CancellationToken.None);

        result.Contenido.Should().NotBeEmpty();
        result.NombreArchivo.Should().Be("orden-compra-OC-2026-0001.pdf");

        _pdfMock.Verify(p => p.Generar(It.Is<OrdenCompraPdfData>(d =>
            d.Numero == "OC-2026-0001" &&
            d.ProveedorNombre == "Acme SA" &&
            d.Items.Count == 2 &&
            d.Neto == oc.Neto &&
            d.Iva == oc.Iva &&
            d.Total == oc.Total)), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Include_Aprobador_When_Not_Approved()
    {
        var oc = OrdenCompraTestFactory.Pendiente();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        await _handler.Handle(new GetOrdenCompraPdfQuery(oc.Id), CancellationToken.None);

        _pdfMock.Verify(p => p.Generar(It.Is<OrdenCompraPdfData>(d =>
            d.AprobadorNombre == null && d.AprobadoEn == null)), Times.Once);
    }

    [Fact]
    public async Task Should_Include_AprobadoEn_When_Approved()
    {
        var oc = OrdenCompraTestFactory.Aprobada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        await _handler.Handle(new GetOrdenCompraPdfQuery(oc.Id), CancellationToken.None);

        _pdfMock.Verify(p => p.Generar(It.Is<OrdenCompraPdfData>(d =>
            d.AprobadoEn != null)), Times.Once);
    }

    [Fact]
    public async Task Should_Sanitize_FileName()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        oc.EnviarAAprobacion("OC/2026/0001");
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(new GetOrdenCompraPdfQuery(oc.Id), CancellationToken.None);

        result.NombreArchivo.Should().Be("orden-compra-OC-2026-0001.pdf");
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Missing()
    {
        var act = async () => await _handler.Handle(new GetOrdenCompraPdfQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
