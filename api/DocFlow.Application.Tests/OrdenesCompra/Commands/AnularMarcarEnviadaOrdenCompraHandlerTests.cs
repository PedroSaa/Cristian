using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.AnularOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.MarcarEnviadaOrdenCompra;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

public class AnularOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AnularOrdenCompraHandler _handler;

    public AnularOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new AnularOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Anular_Enviada_Order()
    {
        var oc = OrdenCompraTestFactory.Aprobada();
        oc.MarcarEnviada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(
            new AnularOrdenCompraCommand(oc.Id, "Proveedor sin stock"), CancellationToken.None);

        result.Estado.Should().Be("Anulada");
        result.MotivoAnulacion.Should().Be("Proveedor sin stock");
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraAnulada")), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Already_Anulada()
    {
        var oc = OrdenCompraTestFactory.Anulada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var act = async () => await _handler.Handle(
            new AnularOrdenCompraCommand(oc.Id, "Otra vez"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public class AnularOrdenCompraValidatorTests
{
    private readonly AnularOrdenCompraValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Require_Motivo(string? motivo)
    {
        _validator.Validate(new AnularOrdenCompraCommand(Guid.NewGuid(), motivo!))
            .IsValid.Should().BeFalse();
    }
}

public class MarcarEnviadaOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly MarcarEnviadaOrdenCompraHandler _handler;

    public MarcarEnviadaOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new MarcarEnviadaOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Mark_Aprobada_As_Enviada()
    {
        var oc = OrdenCompraTestFactory.Aprobada();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(new MarcarEnviadaOrdenCompraCommand(oc.Id), CancellationToken.None);

        result.Estado.Should().Be("Enviada");
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraMarcadaEnviada")), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Not_Aprobada()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var act = async () => await _handler.Handle(new MarcarEnviadaOrdenCompraCommand(oc.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
