using DocFlow.Application.Admin.Usuarios.Commands.ActivarUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.ActivarUsuario;

public class ActivarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<ActivarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    private ActivarUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_currentUserId);
        return new(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ActivaCuentaYPersiste()
    {
        var usuario = CreateInactiveUser();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new ActivarUsuarioCommand(usuario.Id), CancellationToken.None);

        usuario.Activo.Should().BeTrue();
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RegistraAuditoriaConActorAutenticado()
    {
        var usuario = CreateInactiveUser();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new ActivarUsuarioCommand(usuario.Id), CancellationToken.None);

        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r => r.UsuarioId == _currentUserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_LanzaKeyNotFound_SiUsuarioNoExiste()
    {
        _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        var act = async () => await sut.Handle(new ActivarUsuarioCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static SeUsuari CreateInactiveUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: false);
        usuario.VincularPersonal(personal);
        return usuario;
    }
}
