using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.EnviarAprobacionOrdenCompra;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

public class EnviarAprobacionOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IOrdenCompraNumeracionService> _numeracionMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly EnviarAprobacionOrdenCompraHandler _handler;

    public EnviarAprobacionOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());
        _repoMock.Setup(r => r.GetAdjuntosMetadataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _numeracionMock.Setup(n => n.ObtenerSiguienteNumeroAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OC-2026-0777");

        _handler = new EnviarAprobacionOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _numeracionMock.Object,
            _auditoriaMock.Object, _currentUserMock.Object);
    }

    private void SetupOrden(OrdenCompra oc)
        => _repoMock.Setup(r => r.GetByIdAsync(oc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(oc);

    [Fact]
    public async Task Should_Request_Numero_From_Numeracion_Service_On_First_Submission()
    {
        var oc = OrdenCompraTestFactory.Borrador();
        SetupOrden(oc);

        var result = await _handler.Handle(new EnviarAprobacionOrdenCompraCommand(oc.Id), CancellationToken.None);

        result.Estado.Should().Be("PendienteAprobacion");
        result.Numero.Should().Be("OC-2026-0777");
        _numeracionMock.Verify(n => n.ObtenerSiguienteNumeroAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.UpdateAsync(oc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Request_Numero_When_Resubmitting_After_Rechazo()
    {
        var oc = OrdenCompraTestFactory.Pendiente(numero: "OC-2026-0005");
        oc.Rechazar(OrdenCompraTestFactory.AprobadorId, "Corregir precios");
        SetupOrden(oc);

        var result = await _handler.Handle(new EnviarAprobacionOrdenCompraCommand(oc.Id), CancellationToken.None);

        result.Numero.Should().Be("OC-2026-0005");
        _numeracionMock.Verify(n => n.ObtenerSiguienteNumeroAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Burn_Numero_When_Order_Has_No_Items()
    {
        var oc = OrdenCompraTestFactory.BorradorSinItems();
        SetupOrden(oc);

        var act = async () => await _handler.Handle(new EnviarAprobacionOrdenCompraCommand(oc.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _numeracionMock.Verify(n => n.ObtenerSiguienteNumeroAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_Conflict_When_Invalid_State()
    {
        var oc = OrdenCompraTestFactory.Aprobada();
        SetupOrden(oc);

        var act = async () => await _handler.Handle(new EnviarAprobacionOrdenCompraCommand(oc.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _numeracionMock.Verify(n => n.ObtenerSiguienteNumeroAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Orden_Missing()
    {
        var act = async () => await _handler.Handle(new EnviarAprobacionOrdenCompraCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
