using DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Departamentos.Commands.EliminarDepartamento;

public class EliminarDepartamentoCommandHandlerTests
{
    private readonly Mock<IDepartamentoRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<EliminarDepartamentoCommandHandler>> _loggerMock = new();
    private readonly EliminarDepartamentoCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public EliminarDepartamentoCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new EliminarDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    private static Departamento CreateDepartamento() =>
        Departamento.Crear(Guid.NewGuid(), "Test", "TEST-001");

    private static Departamento CreateDepartamentoWithUsuarios()
    {
        var dep = CreateDepartamento();
        var personal = SePersonal.Crear("user", "User", correo: "user@test.com");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "user", "hash", null, dep.Id, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        dep.Usuarios.Add(usuario);
        return dep;
    }

    [Fact]
    public async Task Should_Delete_When_No_Usuarios()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Departamento>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new EliminarDepartamentoCommand(dep.Id);
        await _handler.Handle(cmd, CancellationToken.None);

        _repoMock.Verify(r => r.DeleteAsync(dep), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "EliminarDepartamento" &&
            r.Entidad == "Departamento" &&
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_When_Usuarios_Exist()
    {
        var dep = CreateDepartamentoWithUsuarios();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);

        var cmd = new EliminarDepartamentoCommand(dep.Id);
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Departamento?)null);

        var cmd = new EliminarDepartamentoCommand(id);
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_UserId_Is_Null()
    {
        var dep = CreateDepartamento();
        _repoMock.Setup(r => r.GetByIdAsync(dep.Id)).ReturnsAsync(dep);
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);

        var handler = new EliminarDepartamentoCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
        var cmd = new EliminarDepartamentoCommand(dep.Id);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Departamento>()), Times.Never);
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }
}
