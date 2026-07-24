using DocFlow.Application.Admin.Usuarios.Commands.DesbloquearUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.DesbloquearUsuario;

public class DesbloquearUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    private DesbloquearUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_currentUserId);
        return new(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_DesbloqueaUsuarioYPersisteAuditoria()
    {
        var usuario = CreateUser();
        usuario.Bloquear();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        await sut.Handle(new DesbloquearUsuarioCommand(usuario.Id), CancellationToken.None);

        usuario.EstaBloqueado().Should().BeFalse();
        usuario.IntentosFallidos.Should().Be(0);
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _currentUserId &&
            r.Accion == "DesbloquearUsuario" &&
            r.Entidad == "Usuario" &&
            r.EntidadId == usuario.Id.ToString())), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenUsuarioNotFound()
    {
        var usuarioId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        await sut.Invoking(x => x.Handle(new DesbloquearUsuarioCommand(usuarioId), CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenActorIsNotAuthenticated()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns((Guid?)null);
        var sut = new DesbloquearUsuarioCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);

        await sut.Invoking(x => x.Handle(new DesbloquearUsuarioCommand(Guid.NewGuid()), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static SeUsuari CreateUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        return usuario;
    }
}
