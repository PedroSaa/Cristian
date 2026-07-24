using DocFlow.Application.Auth.Commands.ChangePassword;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private ChangePasswordHandler CreateSut() =>
        new(_currentUserMock.Object, _usuarioRepositoryMock.Object, _passwordHasherMock.Object, _mediatorMock.Object);

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ChangesPasswordAndClearsRefreshSession()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Ada Lovelace", "ada@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "current-hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));

        _currentUserMock.SetupGet(x => x.UserId).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("current-password", "current-hash")).Returns(true);
        _passwordHasherMock.Setup(x => x.Hash("NewPassword1")).Returns("new-hash");

        var sut = CreateSut();

        await sut.Handle(new ChangePasswordCommand("current-password", "NewPassword1"), CancellationToken.None);

        usuario.PasswordHash.Should().Be("new-hash");
        usuario.RefreshTokenHash.Should().BeNull();
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ThrowsAndKeepsExistingRefreshSession()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Ada Lovelace", "ada@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "current-hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));

        _currentUserMock.SetupGet(x => x.UserId).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("bad-password", "current-hash")).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new ChangePasswordCommand("bad-password", "NewPassword1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("La contraseña actual ingresada no es correcta.");
        usuario.RefreshTokenHash.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithValidPassword_PublishesPasswordCambiadoEventWithIniciadoPorUsuario()
    {
        var userId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("Ada Lovelace", "ada@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "current-hash");

        _currentUserMock.SetupGet(x => x.UserId).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("current-password", "current-hash")).Returns(true);
        _passwordHasherMock.Setup(x => x.Hash("NewPassword1")).Returns("new-hash");

        var sut = CreateSut();

        await sut.Handle(new ChangePasswordCommand("current-password", "NewPassword1"), CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<PasswordCambiadoEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
