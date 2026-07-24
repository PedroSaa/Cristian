using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.AprobarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.RechazarOrdenCompra;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

public class AprobarOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AprobarOrdenCompraHandler _handler;

    public AprobarOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.AprobadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new AprobarOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    private void SetupOrden(OrdenCompra oc)
        => _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

    [Fact]
    public async Task Should_Approve_With_CurrentUser_As_Approver()
    {
        var oc = OrdenCompraTestFactory.Pendiente();
        SetupOrden(oc);

        var result = await _handler.Handle(new AprobarOrdenCompraCommand(oc.Id, "OK"), CancellationToken.None);

        result.Estado.Should().Be("Aprobada");
        result.AprobadoPor.Should().Be(OrdenCompraTestFactory.AprobadorId);
        result.ComentarioAprobacion.Should().Be("OK");
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraAprobada")), Times.Once);
    }

    [Fact]
    public async Task Should_Reject_SelfApproval_Of_Own_Order()
    {
        // The current user IS the creator of the order.
        var oc = OrdenCompraTestFactory.Pendiente(creadoPor: OrdenCompraTestFactory.AprobadorId);
        SetupOrden(oc);

        var act = async () => await _handler.Handle(new AprobarOrdenCompraCommand(oc.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*propia*");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<OrdenCompra>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Not_Pendiente()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        SetupOrden(oc);

        var act = async () => await _handler.Handle(new AprobarOrdenCompraCommand(oc.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Missing()
    {
        var act = async () => await _handler.Handle(new AprobarOrdenCompraCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

public class RechazarOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RechazarOrdenCompraHandler _handler;

    public RechazarOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.AprobadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _handler = new RechazarOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Reject_With_Comment()
    {
        var oc = OrdenCompraTestFactory.Pendiente();
        _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

        var result = await _handler.Handle(
            new RechazarOrdenCompraCommand(oc.Id, "Presupuesto insuficiente"), CancellationToken.None);

        result.Estado.Should().Be("Rechazada");
        result.ComentarioAprobacion.Should().Be("Presupuesto insuficiente");
        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraRechazada")), Times.Once);
    }
}

public class RechazarOrdenCompraValidatorTests
{
    private readonly RechazarOrdenCompraValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Require_Comentario(string? comentario)
    {
        var result = _validator.Validate(new RechazarOrdenCompraCommand(Guid.NewGuid(), comentario!));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_With_Comentario()
    {
        var result = _validator.Validate(new RechazarOrdenCompraCommand(Guid.NewGuid(), "Motivo claro"));

        result.IsValid.Should().BeTrue();
    }
}
