using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Commands.ActualizarOrdenCompra;
using DocFlow.Application.OrdenesCompra.Commands.CrearOrdenCompra;
using DocFlow.Application.OrdenesCompra.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Commands;

public class CrearOrdenCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly Mock<IProveedorRepository> _proveedoresMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CrearOrdenCompraHandler _handler;

    public CrearOrdenCompraHandlerTests()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns(OrdenCompraTestFactory.CreadorId);
        _proveedoresMock.Setup(p => p.GetByIdAsync(OrdenCompraTestFactory.ProveedorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrdenCompraTestFactory.Proveedor());

        _handler = new CrearOrdenCompraHandler(
            _repoMock.Object, _proveedoresMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    private static CrearOrdenCompraCommand Comando(IReadOnlyList<OrdenCompraItemInput>? items = null) => new(
        OrdenCompraTestFactory.ProveedorId,
        new DateTime(2026, 7, 1),
        Moneda: "CLP",
        FormaPago: "Transferencia",
        Items: items ?? new[]
        {
            new OrdenCompraItemInput("Notebook", 2m, 500000m),
            new OrdenCompraItemInput("Mouse", 10m, 5000m),
        });

    [Fact]
    public async Task Should_Create_Borrador_With_Items_And_Totals()
    {
        var result = await _handler.Handle(Comando(), CancellationToken.None);

        result.Estado.Should().Be("Borrador");
        result.Numero.Should().BeNull();
        result.ProveedorNombre.Should().Be("Acme SA");
        result.ProveedorRut.Should().Be("12345678-5");
        result.Items.Should().HaveCount(2);
        result.Items[0].NumeroLinea.Should().Be(1);
        result.Neto.Should().Be(1050000m);
        result.Iva.Should().Be(199500m);
        result.Total.Should().Be(1249500m);
        result.CreadoPor.Should().Be(OrdenCompraTestFactory.CreadorId);

        _repoMock.Verify(r => r.AddAsync(
            It.Is<OrdenCompra>(oc => oc.Items.Count == 2 && oc.Total == 1249500m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Write_Audit_Record()
    {
        await _handler.Handle(Comando(), CancellationToken.None);

        _auditoriaMock.Verify(a => a.AddAsync(
            It.Is<RegistroAuditoria>(r => r.Accion == "OrdenCompraCreada" && r.Entidad == "OrdenCompra")),
            Times.Once);
    }

    [Fact]
    public async Task Should_Throw_ValidationException_When_Proveedor_Not_Found()
    {
        _proveedoresMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proveedor?)null);

        var act = async () => await _handler.Handle(Comando(), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .WithMessage("*proveedor*");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<OrdenCompra>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Allow_Empty_Items_In_Draft()
    {
        var result = await _handler.Handle(Comando(items: Array.Empty<OrdenCompraItemInput>()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Neto.Should().Be(0);
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        _currentUserMock.SetupGet(u => u.UserId).Returns((Guid?)null);

        var act = async () => await _handler.Handle(Comando(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

public class CrearOrdenCompraValidatorTests
{
    private readonly CrearOrdenCompraValidator _validator = new();

    [Fact]
    public void Should_Fail_When_ProveedorId_Empty()
    {
        var result = _validator.Validate(new CrearOrdenCompraCommand(Guid.Empty, DateTime.UtcNow));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Item_Cantidad_Not_Positive()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 0m, 100m) });

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Item_Descripcion_Too_Long()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput(new string('x', 301), 1m, 100m) });

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_With_Valid_Command()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 1m, 100m) });

        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    // Column limits: Cantidad numeric(18,4), PrecioUnitario numeric(18,2).
    // Values above these caps would overflow at the database and surface as 500.

    [Fact]
    public void Should_Pass_When_Item_Cantidad_At_Max()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 9_999_999_999.9999m, 100m) });

        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Item_Cantidad_Exceeds_Max()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 10_000_000_000m, 100m) });

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_Item_Precio_At_Max()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 1m, 9_999_999_999_999.99m) });

        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Item_Precio_Exceeds_Max()
    {
        var cmd = new CrearOrdenCompraCommand(
            Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 1m, 10_000_000_000_000m) });

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Actualizar_Should_Fail_When_Item_Cantidad_Exceeds_Max()
    {
        var validator = new ActualizarOrdenCompraValidator();
        var cmd = new ActualizarOrdenCompraCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 10_000_000_000m, 100m) });

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Actualizar_Should_Fail_When_Item_Precio_Exceeds_Max()
    {
        var validator = new ActualizarOrdenCompraValidator();
        var cmd = new ActualizarOrdenCompraCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            Items: new[] { new OrdenCompraItemInput("Item", 1m, 10_000_000_000_000m) });

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
