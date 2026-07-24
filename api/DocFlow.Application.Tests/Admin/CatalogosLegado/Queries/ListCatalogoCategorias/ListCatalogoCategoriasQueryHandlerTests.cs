using DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoCategorias;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Queries.ListCatalogoCategorias;

public class ListCatalogoCategoriasQueryHandlerTests
{
    private readonly Mock<ICatalogoCategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ListCatalogoCategoriasQueryHandler _handler;

    public ListCatalogoCategoriasQueryHandlerTests()
    {
        _handler = new ListCatalogoCategoriasQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsCategoriasWithSubcategoriaCounts()
    {
        var categoria = new CatalogoCategoria(1, "General");
        categoria.Subcategorias.Add(new CatalogoSubcategoria(1, 1, "Sub", null));
        _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { categoria });

        var result = await _handler.Handle(new ListCatalogoCategoriasQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CatCod.Should().Be(1);
        result[0].TotalSubcategorias.Should().Be(1);
    }
}
