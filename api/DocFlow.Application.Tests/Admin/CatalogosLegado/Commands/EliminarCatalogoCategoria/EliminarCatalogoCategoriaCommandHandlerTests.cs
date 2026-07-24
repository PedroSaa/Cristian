using DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarCatalogoCategoria;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.EliminarCatalogoCategoria;

public class EliminarCatalogoCategoriaCommandHandlerTests
{
    private readonly Mock<ICatalogoCategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<EliminarCatalogoCategoriaCommandHandler>> _loggerMock = new();
    private readonly EliminarCatalogoCategoriaCommandHandler _handler;

    public EliminarCatalogoCategoriaCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _handler = new EliminarCatalogoCategoriaCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCategoriaHasSubcategorias_ThrowsConflict()
    {
        var categoria = new CatalogoCategoria(10, "General");
        categoria.Subcategorias.Add(new CatalogoSubcategoria(10, 1, "Sub", null));
        _repoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(categoria);

        var act = () => _handler.Handle(new EliminarCatalogoCategoriaCommand(10), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
