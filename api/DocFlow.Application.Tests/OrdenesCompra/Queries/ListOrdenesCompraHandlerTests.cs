using DocFlow.Application.OrdenesCompra.Queries.ListOrdenesCompra;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.OrdenesCompra.Queries;

public class ListOrdenesCompraHandlerTests
{
    private readonly Mock<IOrdenCompraRepository> _repoMock = new();
    private readonly ListOrdenesCompraHandler _handler;

    public ListOrdenesCompraHandlerTests()
    {
        _handler = new ListOrdenesCompraHandler(_repoMock.Object);
    }

    private static OrdenCompraListRow Row(string? numero = "OC-2026-0001", EstadoOrdenCompra estado = EstadoOrdenCompra.Borrador) => new(
        Guid.NewGuid(),
        numero,
        OrdenCompraTestFactory.ProveedorId,
        "Acme SA",
        new DateTime(2026, 7, 1),
        "CLP",
        1000m,
        190m,
        1190m,
        estado,
        DateTime.UtcNow);

    [Fact]
    public async Task Should_Return_Paginated_Response()
    {
        _repoMock.Setup(r => r.GetListAsync(null, null, null, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([Row()], 25));

        var result = await _handler.Handle(new ListOrdenesCompraQuery(Page: 2, PageSize: 10), CancellationToken.None);

        result.TotalItems.Should().Be(25);
        result.Pagina.Should().Be(2);
        result.TotalPaginas.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProveedorNombre.Should().Be("Acme SA");
        result.Items[0].Total.Should().Be(1190m);
    }

    [Fact]
    public async Task Should_Pass_Filters_To_Repository()
    {
        _repoMock.Setup(r => r.GetListAsync(
                EstadoOrdenCompra.Aprobada,
                OrdenCompraTestFactory.ProveedorId,
                "OC-2026",
                1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0))
            .Verifiable();

        await _handler.Handle(new ListOrdenesCompraQuery(
            Estado: "aprobada",
            ProveedorId: OrdenCompraTestFactory.ProveedorId,
            Search: "OC-2026"), CancellationToken.None);

        _repoMock.Verify();
    }

    [Fact]
    public async Task Should_Return_One_Page_When_Empty()
    {
        _repoMock.Setup(r => r.GetListAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 0));

        var result = await _handler.Handle(new ListOrdenesCompraQuery(), CancellationToken.None);

        result.TotalItems.Should().Be(0);
        result.TotalPaginas.Should().Be(1);
        result.Items.Should().BeEmpty();
    }
}

public class ListOrdenesCompraValidatorTests
{
    private readonly ListOrdenesCompraValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Page_Invalid()
        => _validator.Validate(new ListOrdenesCompraQuery(Page: 0)).IsValid.Should().BeFalse();

    [Fact]
    public void Should_Fail_When_PageSize_Too_Large()
        => _validator.Validate(new ListOrdenesCompraQuery(PageSize: 101)).IsValid.Should().BeFalse();

    [Fact]
    public void Should_Fail_When_Estado_Unknown()
        => _validator.Validate(new ListOrdenesCompraQuery(Estado: "Inexistente")).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("Borrador")]
    [InlineData("pendienteAprobacion")]
    [InlineData("ANULADA")]
    public void Should_Pass_With_Valid_Estado(string estado)
        => _validator.Validate(new ListOrdenesCompraQuery(Estado: estado)).IsValid.Should().BeTrue();
}
