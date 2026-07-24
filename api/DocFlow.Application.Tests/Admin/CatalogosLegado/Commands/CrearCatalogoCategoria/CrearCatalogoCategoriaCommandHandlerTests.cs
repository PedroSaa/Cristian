using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearCatalogoCategoria;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.CrearCatalogoCategoria;

public class CrearCatalogoCategoriaCommandHandlerTests
{
    private readonly Mock<ICatalogoCategoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearCatalogoCategoriaCommandHandler>> _loggerMock = new();
    private readonly CrearCatalogoCategoriaCommandHandler _handler;

    public CrearCatalogoCategoriaCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _handler = new CrearCatalogoCategoriaCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesCategoriaAndReturnsDto()
    {
        _repoMock.Setup(x => x.GetProximoIdAsync()).ReturnsAsync(10);
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<CatalogoCategoria>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearCatalogoCategoriaCommand("General"), CancellationToken.None);

        result.CatCod.Should().Be(10);
        result.CatDesc.Should().Be("General");
    }

    [Fact]
    public async Task Handle_AutogeneratesCatCod_UsingProximoId()
    {
        _repoMock.Setup(x => x.GetProximoIdAsync()).ReturnsAsync(23);
        CatalogoCategoria? persisted = null;
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<CatalogoCategoria>()))
            .Callback<CatalogoCategoria>(e => persisted = e)
            .Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearCatalogoCategoriaCommand("Contratos"), CancellationToken.None);

        result.CatCod.Should().Be(23);
        persisted!.CatCod.Should().Be(23);
        _repoMock.Verify(x => x.GetProximoIdAsync(), Times.Once);
    }
}
