using DocFlow.Application.Admin.Departamentos.Commands.ActualizarDepartamento;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.ActualizarDepartamento;

public class ActualizarDepartamentoCommandHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ActualizarDepartamentoCommandHandler>> _loggerMock = new();
    private readonly ActualizarDepartamentoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public ActualizarDepartamentoCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new ActualizarDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    private static Departamento CreateDepartamento() =>
        Departamento.Crear(Guid.NewGuid(), "Original Name", "ORIG-001");

    [Fact]
    public async Task Should_Update_Nombre_And_Codigo()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.ExistsByNombreAsync("Updated Name")).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCodigoAsync("UPD-001")).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Departamento>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarDepartamentoCommand(dep.Id, "Updated Name", "UPD-001");
        await _handler.Handle(cmd, CancellationToken.None);

        dep.Nombre.Should().Be("Updated Name");
        dep.Codigo.Should().Be("UPD-001");
        _repoMock.Verify(r => r.UpdateAsync(dep), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Departamento?)null);

        var cmd = new ActualizarDepartamentoCommand(id, "Test", "T-001");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Should_Throw_When_Codigo_Conflicts_With_Another()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.ExistsByNombreAsync("Updated")).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCodigoAsync("CONFLICT-001")).ReturnsAsync(true);

        var cmd = new ActualizarDepartamentoCommand(dep.Id, "Updated", "CONFLICT-001");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Throw_When_Nombre_Conflicts_With_Another()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.ExistsByNombreAsync("CONFLICT-NAME")).ReturnsAsync(true);

        var cmd = new ActualizarDepartamentoCommand(dep.Id, "CONFLICT-NAME", "UPD-001");
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
