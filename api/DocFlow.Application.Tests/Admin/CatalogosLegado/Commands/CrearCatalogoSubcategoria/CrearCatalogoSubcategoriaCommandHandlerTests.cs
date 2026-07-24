using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoSubcategoria;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.CrearCatalogoSubcategoria;

public class CrearCatalogoSubcategoriaCommandHandlerTests
{
    private readonly Mock<ICatalogoCategoriaRepository> _categoriaRepoMock = new(MockBehavior.Strict);
    private readonly Mock<ICatalogoSubcategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearCatalogoSubcategoriaCommandHandler>> _loggerMock = new();
    private readonly CrearCatalogoSubcategoriaCommandHandler _handler;

    public CrearCatalogoSubcategoriaCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _handler = new CrearCatalogoSubcategoriaCommandHandler(
            _categoriaRepoMock.Object,
            _repoMock.Object,
            _auditoriaMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesSubcategoriaAndReturnsDto()
    {
        var categoria = new CatalogoCategoria(1, "General");
        _categoriaRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(categoria);
        _repoMock.Setup(x => x.GetProximoIdSubcategoriaAsync(1)).ReturnsAsync((short)2);
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<CatalogoSubcategoria>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearCatalogoSubcategoriaCommand(1, "Mesa", "Desc"), CancellationToken.None);

        result.CatCod.Should().Be(1);
        result.IdSubcategoria.Should().Be(2);
        result.SubcatNombre.Should().Be("Mesa");
    }

    [Fact]
    public async Task Handle_AutogeneratesIdSubcategoria_PerCategoria()
    {
        var categoria = new CatalogoCategoria(7, "Contratos");
        _categoriaRepoMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(categoria);
        _repoMock.Setup(x => x.GetProximoIdSubcategoriaAsync(7)).ReturnsAsync((short)4);
        CatalogoSubcategoria? persisted = null;
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<CatalogoSubcategoria>()))
            .Callback<CatalogoSubcategoria>(e => persisted = e)
            .Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearCatalogoSubcategoriaCommand(7, "Arriendo", null), CancellationToken.None);

        result.IdSubcategoria.Should().Be((short)4);
        persisted!.CatCod.Should().Be(7);
        persisted.IdSubcategoria.Should().Be((short)4);
        _repoMock.Verify(x => x.GetProximoIdSubcategoriaAsync(7), Times.Once);
    }
}
