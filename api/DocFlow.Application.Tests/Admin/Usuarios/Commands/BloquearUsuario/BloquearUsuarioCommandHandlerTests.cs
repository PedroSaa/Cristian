using DocFlow.Application.Admin.Usuarios.Commands.BloquearUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.BloquearUsuario;

public class BloquearUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    private BloquearUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_currentUserId);
        return new(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_BloqueaUsuarioRevocaSesionYPersisteAuditoria()
    {
        var usuario = CreateUser(isAdmin: false);
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new BloquearUsuarioCommand(usuario.Id), CancellationToken.None);

        usuario.EstaBloqueado().Should().BeTrue();
        usuario.AuthSessionVersion.Should().Be(1);
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _currentUserId &&
            r.Accion == "BloquearUsuario" &&
            r.Entidad == "Usuario" &&
            r.EntidadId == usuario.Id.ToString() &&
            r.Detalle.Contains(usuario.Personal!.Correo)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenActorIsNotAuthenticated()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns((Guid?)null);
        var sut = new BloquearUsuarioCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);

        await sut.Invoking(x => x.Handle(new BloquearUsuarioCommand(Guid.NewGuid()), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _repoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenUsuarioNotFound()
    {
        var usuarioId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        await sut.Invoking(x => x.Handle(new BloquearUsuarioCommand(usuarioId), CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();

        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenBlockingOwnAccount()
    {
        var usuario = CreateUser(isAdmin: false, id: _currentUserId);
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Invoking(x => x.Handle(new BloquearUsuarioCommand(usuario.Id), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No puedes bloquear tu propia cuenta.");

        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<SePersonal>(), It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenBlockingLastActiveAdministrator()
    {
        var usuario = CreateUser(isAdmin: true);
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _repoMock.Setup(x => x.CountActiveAdministratorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        await sut.Invoking(x => x.Handle(new BloquearUsuarioCommand(usuario.Id), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No puedes bloquear al último administrador activo.");

        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<SePersonal>(), It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    private static SeUsuari CreateUser(bool isAdmin, Guid? id = null)
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(id ?? Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        if (isAdmin)
            typeof(SeUsuari).GetProperty(nameof(SeUsuari.Rol))!.SetValue(usuario, new Rol(Guid.NewGuid(), "Administrador", "Administrador del sistema"));
        return usuario;
    }
}
