using DocFlow.Application.Auth.Commands.Login;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Common.Exceptions;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class LoginCommandLockoutTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    public LoginCommandLockoutTests()
    {
        _securityPolicyMock.Setup(x => x.GetLockoutMaxAttempts()).Returns(5);
        _securityPolicyMock.Setup(x => x.GetLockoutDurationMinutes()).Returns(30);
    }

    private LoginCommandHandler CreateSut() =>
        new(_usuarioRepositoryMock.Object, _passwordHasherMock.Object, _jwtProviderMock.Object, _mediatorMock.Object, _currentUserMock.Object, _totpServiceMock.Object, _securityPolicyMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithLockedAccount_ThrowsUnauthorizedWithLockedMessage()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.Bloquear();

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "any-password"), CancellationToken.None);

        await act.Should().ThrowAsync<LoginFailedException>().WithMessage("*bloqueada*");
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_IncrementsFailedLoginAttempts()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("wrong-password", "$2b$hash")).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<LoginFailedException>();
        usuario.IntentosFallidos.Should().Be(1);
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_OnFifthFailedAttempt_LocksAccount()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash", failedLoginAttempts: 4);

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("wrong-password", "$2b$hash")).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<LoginFailedException>();
        usuario.IntentosFallidos.Should().Be(5);
        usuario.BloqueadoHasta.Should().NotBeNull();
        usuario.EstaBloqueado().Should().BeTrue();
        _mediatorMock.Verify(x => x.Publish(It.Is<UsuarioBloqueadoEvent>(e => e.Origen == "auto" && e.IntentosFallidos == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_PublishesLoginFallidoEvent()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("wrong-password", "$2b$hash")).Returns(false);
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("1.2.3.4");
        _currentUserMock.SetupGet(x => x.UserAgent).Returns("ua-test");

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "wrong-password"), CancellationToken.None);
        await act.Should().ThrowAsync<LoginFailedException>();

        _mediatorMock.Verify(x => x.Publish(It.Is<LoginFallidoEvent>(e =>
            e.UsuarioId == usuario.UsuarioId
            && e.Motivo == "Contraseña incorrecta"
            && e.IpAddress == "1.2.3.4"
            && e.UserAgent == "ua-test"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownIdentifier_PublishesLoginFallidoEvent_WithEmptyUserId()
    {
        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ghost@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("1.2.3.4");

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("ghost@docflow.cl", "any"), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        _mediatorMock.Verify(x => x.Publish(It.Is<LoginFallidoEvent>(e =>
            e.UsuarioId == Guid.Empty
            && e.Identificador == "ghost@docflow.cl"
            && e.Motivo == "Identificador inexistente"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ResetsFailedLoginAttempts()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash", failedLoginAttempts: 2);

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("correct-password", "$2b$hash")).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>())).Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();

        await sut.Handle(new LoginCommand("test@docflow.cl", "correct-password"), CancellationToken.None);

        usuario.IntentosFallidos.Should().Be(0);
        usuario.BloqueadoHasta.Should().BeNull();
        usuario.UltimoAcceso.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithConfigDrivenLockoutThreshold_UsesRuntimeCountForRemainingAttempts()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("wrong", "$2b$hash")).Returns(false);
        _securityPolicyMock.Setup(x => x.GetLockoutMaxAttempts()).Returns(3);
        _securityPolicyMock.Setup(x => x.GetLockoutDurationMinutes()).Returns(30);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<LoginFailedException>();
        usuario.IntentosFallidos.Should().Be(1);
    }
}
