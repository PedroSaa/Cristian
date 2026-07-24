using DocFlow.Application.Admin.Departamentos.Commands.ActivarDepartamento;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.ActivarDepartamento;

public class ActivarDepartamentoCommandHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ActivarDepartamentoCommandHandler>> _loggerMock = new();
    private readonly ActivarDepartamentoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public ActivarDepartamentoCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new ActivarDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    private static Departamento CreateInactivo()
    {
        var dep = Departamento.Crear(Guid.NewGuid(), "Test", "TEST-001");
        dep.Desactivar();
        return dep;
    }

    [Fact]
    public async Task Should_Activate_And_Persist()
    {
        var dep = CreateInactivo();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Departamento>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActivarDepartamentoCommand(dep.Id);
        await _handler.Handle(cmd, CancellationToken.None);

        dep.Activo.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(dep), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Departamento?)null);

        var cmd = new ActivarDepartamentoCommand(id);
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
