using DocFlow.Application.Admin.CatalogosLegado.Queries.GetCatalogoSubcategoria;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Queries.GetCatalogoSubcategoria;

public class GetCatalogoSubcategoriaQueryHandlerTests
{
    private readonly Mock<ICatalogoSubcategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetCatalogoSubcategoriaQueryHandler _handler;

    public GetCatalogoSubcategoriaQueryHandlerTests()
    {
        _handler = new GetCatalogoSubcategoriaQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSubcategoriaDto()
    {
        var categoria = new CatalogoCategoria(1, "General");
        var subcategoria = new CatalogoSubcategoria(1, 2, "Mesa", null);
        typeof(CatalogoSubcategoria).GetProperty(nameof(CatalogoSubcategoria.Categoria))!
            .SetValue(subcategoria, categoria);
        _repoMock.Setup(x => x.GetByIdAsync(1, 2)).ReturnsAsync(subcategoria);

        var result = await _handler.Handle(new GetCatalogoSubcategoriaQuery(1, 2), CancellationToken.None);

        result.CategoriaDesc.Should().Be("General");
        result.SubcatNombre.Should().Be("Mesa");
    }
}
