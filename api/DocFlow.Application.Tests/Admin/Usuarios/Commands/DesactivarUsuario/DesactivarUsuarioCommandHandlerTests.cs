using DocFlow.Application.Admin.Usuarios.Commands.DesactivarUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.DesactivarUsuario;

public class DesactivarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DesactivarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    private DesactivarUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_currentUserId);
        return new(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_DesactivaCuentaYPersiste()
    {
        var usuario = CreateUser();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new DesactivarUsuarioCommand(usuario.Id), CancellationToken.None);

        usuario.Activo.Should().BeFalse();
        usuario.AuthSessionVersion.Should().Be(1);
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SeUsuari CreateUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        return usuario;
    }
}
