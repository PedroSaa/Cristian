using DocFlow.Application.Admin.Usuarios.Commands.ActualizarUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.ActualizarUsuario;

public class ActualizarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<ActualizarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<IRolRepository> _rolRepoMock = new();

    private readonly Guid _userId = Guid.NewGuid();

    private ActualizarUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_userId);
        return new(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object, _rolRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ActualizaPersonalYRol()
    {
        var usuario = CreateUser();
        var adminRolId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _repoMock.Setup(x => x.ExistsByCorreoAsync("final@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _rolRepoMock.Setup(x => x.GetByNombreAsync("Administrador")).ReturnsAsync(new Rol(adminRolId, "Administrador", "Administrador del sistema"));

        var sut = CreateSut();

        await sut.Handle(new ActualizarUsuarioCommand(usuario.Id, "Nombre", "Final", "Uno", null, null, "Administrador", null, "final@docflow.cl", "11.111.111-1"), CancellationToken.None);

        usuario.Personal!.Nombres.Should().Be("Nombre");
        usuario.Personal.Correo.Should().Be("final@docflow.cl");
        usuario.Personal.Rut.Should().Be("11.111.111-1");
        usuario.Personal.ApellidoPaterno.Should().Be("Final");
        usuario.Personal.ApellidoMaterno.Should().Be("Uno");
        usuario.RolId.Should().Be(adminRolId);
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validate_ConRutMayorA20_Falla()
    {
        var validator = new ActualizarUsuarioCommandValidator();
        var cmd = new ActualizarUsuarioCommand(Guid.NewGuid(), "Nombre", "Ap", "Am", null, null, "Usuario", null, "user@docflow.cl", new string('1', 21));

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rut");
    }

    [Fact]
    public void Validate_ConRutDe20_Pasa()
    {
        var validator = new ActualizarUsuarioCommandValidator();
        var cmd = new ActualizarUsuarioCommand(Guid.NewGuid(), "Nombre", "Ap", "Am", null, null, "Usuario", null, "user@docflow.cl", new string('1', 20));

        var result = validator.Validate(cmd);

        result.Errors.Should().NotContain(e => e.PropertyName == "Rut");
    }

    private static SeUsuari CreateUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl", rut: "12.345.678-9");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        return usuario;
    }
}
