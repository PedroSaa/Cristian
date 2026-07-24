using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.DesvincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.VincularMercadoPublicoOrdenCompra;
using DocFlow.Application.OrdenesCompra.Interfaces;
using DocFlow.Application.OrdenesCompra.Queries.BuscarOrdenMercadoPublico;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using FluentValidation;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

internal static class MercadoPublicoTestData
{
    public static MercadoPublicoOrdenDto Orden(string codigo = "1123-109-SE13") => new(
        Codigo: codigo,
        Nombre: "Mantención Áreas verdes Junio 2013",
        Estado: "Aceptada",
        FechaCreacion: "2013-07-05T12:59:15.443",
        CompradorNombre: "INSTITUTO DE DESARROLLO AGROPECUARIO",
        CompradorRut: "61.307.000-1",
        ProveedorNombre: "MARGOT DEL ROSARIO NÚÑEZ SILVA",
        ProveedorRut: "7.445.387-2",
        MontoTotal: 110908m,
        Items: [new MercadoPublicoOrdenItemDto("Servicios de jardinería", 1m, 46200m)]);
}

// ── Query: BuscarOrdenMercadoPublico ──

public class BuscarOrdenMercadoPublicoHandlerTests
{
    private readonly Mock<IMercadoPublicoService> _serviceMock = new();
    private readonly BuscarOrdenMercadoPublicoHandler _handler;

    public BuscarOrdenMercadoPublicoHandlerTests()
        => _handler = new BuscarOrdenMercadoPublicoHandler(_serviceMock.Object);

    [Fact]
    public async Task Should_Return_Dto_When_Portal_Finds_Order()
    {
        var orden = MercadoPublicoTestData.Orden();
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync("1123-109-SE13", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orden);

        var result = await _handler.Handle(
            new BuscarOrdenMercadoPublicoQuery("1123-109-SE13"), CancellationToken.None);

        result.Should().BeEquivalentTo(orden);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Portal_Returns_Null()
    {
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MercadoPublicoOrdenDto?)null);

        var act = async () => await _handler.Handle(
            new BuscarOrdenMercadoPublicoQuery("0000-0-XX00"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Should_Propagate_InvalidOperation_When_Ticket_Missing()
    {
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("El ticket de Mercado Público no está configurado."));

        var act = async () => await _handler.Handle(
            new BuscarOrdenMercadoPublicoQuery("1123-109-SE13"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public class BuscarOrdenMercadoPublicoValidatorTests
{
    private readonly BuscarOrdenMercadoPublicoValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Require_Codigo(string? codigo)
    {
        _validator.Validate(new BuscarOrdenMercadoPublicoQuery(codigo!))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_Codigo_Longer_Than_40()
    {
        _validator.Validate(new BuscarOrdenMercadoPublicoQuery(new string('X', 41)))
            .IsValid.Should().BeFalse();
    }
}

// ── Command: VincularMercadoPublicoOrdenCompra ──

public class VincularMercadoPublicoOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IMercadoPublicoService> _serviceMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly VincularMercadoPublicoOrdenCompraHandler _handler;

    public VincularMercadoPublicoOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new VincularMercadoPublicoOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _serviceMock.Object,
            _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Link_When_Portal_Confirms_Codigo()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync("1123-109-SE13", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MercadoPublicoTestData.Orden());

        var result = await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(oc.Id, "1123-109-SE13"), CancellationToken.None);

        result.CodigoMercadoPublico.Should().Be("1123-109-SE13");
        _repoMock.Verify(r => r.UpdateAsync(oc, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraVinculadaMercadoPublico")), Times.Once);
    }

    [Fact]
    public async Task Should_Trim_Codigo_Before_Portal_Lookup()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync("1123-109-SE13", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MercadoPublicoTestData.Orden());

        await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(oc.Id, "  1123-109-SE13  "), CancellationToken.None);

        _serviceMock.Verify(s => s.BuscarPorCodigoAsync("1123-109-SE13", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Orden_Missing()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.OrdenesCompra.OrdenCompra?)null);

        var act = async () => await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(Guid.NewGuid(), "1123-109-SE13"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _serviceMock.Verify(
            s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_Validation_When_Codigo_Not_In_Portal()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MercadoPublicoOrdenDto?)null);

        var act = async () => await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(oc.Id, "0000-0-XX00"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.OrdenesCompra.OrdenCompra>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_Validation_When_Orden_Anulada()
    {
        var oc = OrdenCompraTestFactory.Anulada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MercadoPublicoTestData.Orden());

        var act = async () => await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(oc.Id, "1123-109-SE13"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Should_Propagate_InvalidOperation_When_Portal_Unavailable()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);
        _serviceMock.Setup(s => s.BuscarPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mercado Público no está disponible."));

        var act = async () => await _handler.Handle(
            new VincularMercadoPublicoOrdenCompraCommand(oc.Id, "1123-109-SE13"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public class VincularMercadoPublicoOrdenCompraValidatorTests
{
    private readonly VincularMercadoPublicoOrdenCompraValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Require_Codigo(string? codigo)
    {
        _validator.Validate(new VincularMercadoPublicoOrdenCompraCommand(Guid.NewGuid(), codigo!))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Require_OrdenCompraId()
    {
        _validator.Validate(new VincularMercadoPublicoOrdenCompraCommand(Guid.Empty, "1123-109-SE13"))
            .IsValid.Should().BeFalse();
    }
}

// ── Command: DesvincularMercadoPublicoOrdenCompra ──

public class DesvincularMercadoPublicoOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DesvincularMercadoPublicoOrdenCompraHandler _handler;

    public DesvincularMercadoPublicoOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new DesvincularMercadoPublicoOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Unlink_And_Audit()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        oc.VincularMercadoPublico("1123-109-SE13");
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(
            new DesvincularMercadoPublicoOrdenCompraCommand(oc.Id), CancellationToken.None);

        result.CodigoMercadoPublico.Should().BeNull();
        _repoMock.Verify(r => r.UpdateAsync(oc, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraDesvinculadaMercadoPublico")), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Orden_Missing()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.OrdenesCompra.OrdenCompra?)null);

        var act = async () => await _handler.Handle(
            new DesvincularMercadoPublicoOrdenCompraCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
