using DocFlow.Application.Admin.Auditoria.Queries.GetValoresFiltro;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Queries.GetValoresFiltro;

public class GetValoresFiltroQueryHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetValoresFiltroQueryHandler _handler;

    public GetValoresFiltroQueryHandlerTests()
    {
        _handler = new GetValoresFiltroQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_ValoresFiltro_With_Distinct_Values()
    {
        // Arrange
        var expected = new ValoresFiltro(
            ["Login", "CrearUsuario", "Eliminar"],
            ["Usuario", "Documento", "Configuracion"]);

        _repoMock
            .Setup(r => r.GetValoresFiltroAsync())
            .ReturnsAsync(expected);

        var query = new GetValoresFiltroQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Acciones.Should().BeEquivalentTo(expected.Acciones);
        result.Entidades.Should().BeEquivalentTo(expected.Entidades);
        _repoMock.Verify(r => r.GetValoresFiltroAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Return_Empty_Lists_When_No_Audit_Entries()
    {
        // Arrange
        var expected = new ValoresFiltro([], []);

        _repoMock
            .Setup(r => r.GetValoresFiltroAsync())
            .ReturnsAsync(expected);

        var query = new GetValoresFiltroQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Acciones.Should().BeEmpty();
        result.Entidades.Should().BeEmpty();
        _repoMock.Verify(r => r.GetValoresFiltroAsync(), Times.Once);
    }
}
