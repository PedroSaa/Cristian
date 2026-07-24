using DocFlow.Application.Admin.CatalogosLegado.Commands.CrearSeremTipo;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado.Commands.CrearSeremTipo;

public class CrearSeremTipoCommandHandlerTests
{
    private readonly Mock<ISeremTipoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<CrearSeremTipoCommandHandler>> _loggerMock = new();
    private readonly CrearSeremTipoCommandHandler _handler;

    public CrearSeremTipoCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _handler = new CrearSeremTipoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesTipo_AndReturnsDto()
    {
        _repoMock.Setup(x => x.ExistsAsync("A01")).ReturnsAsync(false);
        _repoMock.Setup(x => x.CreateAsync(It.IsAny<SeremTipo>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new CrearSeremTipoCommand("A01", "Municipales"), CancellationToken.None);

        result.RemTipo.Should().Be("A01");
        result.RemDesc.Should().Be("Municipales");
    }

    [Fact]
    public async Task Handle_WhenTipoAlreadyExists_ThrowsConflict()
    {
        _repoMock.Setup(x => x.ExistsAsync("A01")).ReturnsAsync(true);

        var act = () => _handler.Handle(new CrearSeremTipoCommand("A01", "Municipales"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
