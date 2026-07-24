using DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoSubcategorias;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Queries.ListCatalogoSubcategorias;

public class ListCatalogoSubcategoriasQueryHandlerTests
{
    private readonly Mock<ICatalogoSubcategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListCatalogoSubcategoriasQueryHandler _handler;

    public ListCatalogoSubcategoriasQueryHandlerTests()
    {
        _handler = new ListCatalogoSubcategoriasQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFilteredSubcategorias()
    {
        var categoria = new CatalogoCategoria(1, "General");
        var subcategoria = new CatalogoSubcategoria(1, 1, "Mesa", null);
        typeof(CatalogoSubcategoria).GetProperty(nameof(CatalogoSubcategoria.Categoria))!
            .SetValue(subcategoria, categoria);

        _repoMock.Setup(x => x.GetAllAsync(1)).ReturnsAsync(new[] { subcategoria });

        var result = await _handler.Handle(new ListCatalogoSubcategoriasQuery(1), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CategoriaDesc.Should().Be("General");
    }
}
