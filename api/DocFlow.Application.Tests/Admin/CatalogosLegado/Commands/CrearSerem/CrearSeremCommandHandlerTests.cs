using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSerem;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.CrearSerem;

public class CrearSeremCommandHandlerTests
{
    private readonly Mock<ISeremRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<ISeremTipoRepository> _tipoRepoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearSeremCommandHandler>> _loggerMock = new();
    private readonly CrearSeremCommandHandler _handler;

    public CrearSeremCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _handler = new CrearSeremCommandHandler(_repoMock.Object, _tipoRepoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesRemitente_WhenTipoExists()
    {
        var tipo = new SeremTipo("A01", "Municipales");
        _repoMock.Setup(x => x.ExistsAsync("REM-001")).ReturnsAsync(false);
        _tipoRepoMock.Setup(x => x.GetByIdAsync("A01")).ReturnsAsync(tipo);
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<Serem>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearSeremCommand("REM-001", "A01", "Municipalidad"), CancellationToken.None);

        result.RemCod.Should().Be("REM-001");
        result.RemTipo.Should().Be("A01");
        result.RemTipoDesc.Should().Be("Municipales");
    }

    [Fact]
    public async Task Handle_WhenTipoMissing_ThrowsNotFound()
    {
        _repoMock.Setup(x => x.ExistsAsync("REM-002")).ReturnsAsync(false);
        _tipoRepoMock.Setup(x => x.GetByIdAsync("A01")).ReturnsAsync((SeremTipo?)null);

        var act = () => _handler.Handle(new CrearSeremCommand("REM-002", "A01", "Municipalidad"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
