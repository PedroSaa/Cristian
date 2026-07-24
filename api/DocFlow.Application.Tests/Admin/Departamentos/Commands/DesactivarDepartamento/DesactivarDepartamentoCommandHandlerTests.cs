using DocFlow.Application.Admin.Departamentos.Commands.DesactivarDepartamento;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.DesactivarDepartamento;

public class DesactivarDepartamentoCommandHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<DesactivarDepartamentoCommandHandler>> _loggerMock = new();
    private readonly DesactivarDepartamentoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public DesactivarDepartamentoCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new DesactivarDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    private static Departamento CreateActivo() =>
        Departamento.Crear(Guid.NewGuid(), "Test", "TEST-001");

    [Fact]
    public async Task Should_Deactivate_And_Persist()
    {
        var dep = CreateActivo();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Departamento>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new DesactivarDepartamentoCommand(dep.Id);
        await _handler.Handle(cmd, CancellationToken.None);

        dep.Activo.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(dep), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Departamento?)null);

        var cmd = new DesactivarDepartamentoCommand(id);
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
