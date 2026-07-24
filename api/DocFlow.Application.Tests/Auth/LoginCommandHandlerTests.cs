using DocFlow.Application.Auth.Commands.Login;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Exceptions;
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

public class LoginCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    public LoginCommandHandlerTests()
    {
        _securityPolicyMock.Setup(x => x.GetLockoutMaxAttempts()).Returns(5);
        _securityPolicyMock.Setup(x => x.GetLockoutDurationMinutes()).Returns(30);
    }

    private LoginCommandHandler CreateSut() =>
        new(_usuarioRepositoryMock.Object, _passwordHasherMock.Object, _jwtProviderMock.Object, _mediatorMock.Object, _currentUserMock.Object, _totpServiceMock.Object, _securityPolicyMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsCanonicalSessionContract()
    {
        var departamentoId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            departamentoId,
            passwordHash: "$2b$stored-hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ada@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("super-secret", "$2b$stored-hash")).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>()))
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("10.0.0.1");

        var sut = CreateSut();

        var result = await sut.Handle(new LoginCommand("ada@docflow.cl", "super-secret"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Nombre.Should().Be("Ada Lovelace");
        result.User.Email.Should().Be("ada@docflow.cl");
        result.User.Rol.Should().Be(nameof(RolUsuario.Administrador));
        usuario.RefreshTokenHash.Should().NotBeNull();
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_PublishesEventWithRealIp()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "$2b$stored-hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ada@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("super-secret", "$2b$stored-hash")).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>()))
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("192.168.1.100");

        var sut = CreateSut();

        await sut.Handle(new LoginCommand("ada@docflow.cl", "super-secret"), CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(
            It.Is<UsuarioAutenticadoEvent>(e => e.Ip == "192.168.1.100" && e.Metodo == "password"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsSafeUnauthorizedMessage()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "$2b$stored-hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ada@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("wrong-password", "$2b$stored-hash")).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("ada@docflow.cl", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<LoginFailedException>()
            .WithMessage("Identificador o contraseña incorrectos.");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsSafeUnauthorizedMessageWithoutLeakingState()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "$2b$stored-hash");
        usuario.Desactivar();

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ada@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("super-secret", "$2b$stored-hash")).Returns(true);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("ada@docflow.cl", "super-secret"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Identificador o contraseña incorrectos.");
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMfaEnabledUser_ThrowsMfaRequiredException()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Test User",
            "test@docflow.cl",
            nameof(RolUsuario.Usuario),
            AuthUserFactory.UsuarioPermissions(),
            passwordHash: "$2b$hash",
            mfaEnabled: true,
            mfaSecretKey: "JBSWY3DPEHPK3PXP");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("test@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("password", "$2b$hash")).Returns(true);
        _jwtProviderMock.Setup(x => x.GenerateMfaToken(usuario.UsuarioId)).Returns("mfa-token-xyz");

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("test@docflow.cl", "password"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<MfaRequiredException>();
        ex.Which.MfaToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenPolicyRequiresMfaAndUserHasNotEnrolled_ReturnsSetupOnlyState()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "$2b$stored-hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("ada@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("super-secret", "$2b$stored-hash")).Returns(true);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForAdministrators()).Returns(true);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForOtherUsers()).Returns(false);
        _jwtProviderMock.Setup(x => x.GenerateMfaToken(usuario.UsuarioId)).Returns("setup-token");

        var sut = CreateSut();

        var result = await sut.Handle(new LoginCommand("ada@docflow.cl", "super-secret"), CancellationToken.None);

        result.AuthState.Should().Be(AuthState.MfaSetupRequired);
        result.SetupToken.Should().Be("setup-token");
        result.CanLogout.Should().BeTrue();
        result.AccessToken.Should().BeEmpty();
        result.RefreshToken.Should().BeEmpty();
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
    }
}
