using DocFlow.Application.Admin.Roles.Commands.EliminarRol;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Commands.EliminarRol;

public class EliminarRolCommandHandlerTests
{
    private readonly Mock<IRolRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<EliminarRolCommandHandler>> _loggerMock = new();
    private readonly EliminarRolCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public EliminarRolCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _handler = new EliminarRolCommandHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    private static Rol CreateRol(string nombre = "Supervisor", bool esSistema = false)
        => new(Guid.NewGuid(), nombre, "Descripción", esSistema);

    private static Rol CreateRolWithUsuarios(int count = 3)
    {
        var rol = CreateRol();
        var usuarios = Enumerable.Range(1, count)
            .Select(i =>
            {
                var personal = SePersonal.Crear($"user{i}", $"User{i}", correo: $"user{i}@test.com");
                var usuario = SeUsuari.Crear(Guid.NewGuid(), $"user{i}", "hash", null, null, estadoCuenta: true);
                usuario.VincularPersonal(personal);
                return usuario;
            })
            .ToList();
        foreach (var usuario in usuarios)
            rol.Usuarios.Add(usuario);
        return rol;
    }

    [Fact]
    public async Task Should_Delete_When_No_Usuarios_And_Not_Sistema()
    {
        // Arrange
        var rol = CreateRol();
        _repoMock.Setup(r => r.GetByIdWithUsuariosAsync(rol.Id)).ReturnsAsync(rol);
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Rol>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new EliminarRolCommand(rol.Id);

        // Act
        await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(rol), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "RolEliminado" &&
            r.Entidad == "Rol" &&
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_When_EsSistema()
    {
        // Arrange
        var rol = CreateRol(esSistema: true);
        _repoMock.Setup(r => r.GetByIdWithUsuariosAsync(rol.Id)).ReturnsAsync(rol);

        var cmd = new EliminarRolCommand(rol.Id);

        // Act
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede eliminar un rol del sistema.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_When_Usuarios_Assigned()
    {
        // Arrange
        var rol = CreateRolWithUsuarios(count: 3);
        _repoMock.Setup(r => r.GetByIdWithUsuariosAsync(rol.Id)).ReturnsAsync(rol);

        var cmd = new EliminarRolCommand(rol.Id);

        // Act
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No se puede eliminar un rol con 3 usuarios asignados.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Not_Found()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdWithUsuariosAsync(id)).ReturnsAsync((Rol?)null);

        var cmd = new EliminarRolCommand(id);

        // Act
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Rol {id} no encontrado.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_When_UserId_Is_Null()
    {
        // Arrange
        var rol = CreateRol();
        _repoMock.Setup(r => r.GetByIdWithUsuariosAsync(rol.Id)).ReturnsAsync(rol);
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);

        var handler = new EliminarRolCommandHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
        var cmd = new EliminarRolCommand(rol.Id);

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Rol>()), Times.Never);
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }
}
