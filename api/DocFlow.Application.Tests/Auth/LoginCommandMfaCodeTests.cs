using DocFlow.Application.Auth.Commands.Login;
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

public class LoginCommandMfaCodeTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    public LoginCommandMfaCodeTests()
    {
        _securityPolicyMock.Setup(x => x.GetLockoutMaxAttempts()).Returns(5);
        _securityPolicyMock.Setup(x => x.GetLockoutDurationMinutes()).Returns(30);
    }

    private LoginCommandHandler CreateSut() =>
        new(_usuarioRepositoryMock.Object, _passwordHasherMock.Object, _jwtProviderMock.Object, _mediatorMock.Object, _currentUserMock.Object, _totpServiceMock.Object, _securityPolicyMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithMfaEnabledAndValidMfaCode_IssuesTokens()
    {
        var usuario = AuthUserFactory.CreateUser("MFA User", "mfa@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa("JBSWY3DPEHPK3PXP");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("mfa@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("password", "$2b$hash")).Returns(true);
        _totpServiceMock.Setup(x => x.ValidateCode("JBSWY3DPEHPK3PXP", "123456")).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>())).Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("10.0.0.1");

        var sut = CreateSut();

        var result = await sut.Handle(new LoginCommand("mfa@docflow.cl", "password", "123456"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("mfa@docflow.cl");
        _jwtProviderMock.Verify(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.Is<UsuarioAutenticadoEvent>(e => e.Metodo == "mfa"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMfaEnabledAndNoMfaCode_ThrowsMfaRequired()
    {
        var usuario = AuthUserFactory.CreateUser("MFA User", "mfa@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa("JBSWY3DPEHPK3PXP");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("mfa@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("password", "$2b$hash")).Returns(true);
        _jwtProviderMock.Setup(x => x.GenerateMfaToken(usuario.UsuarioId)).Returns("mfa-token-xyz");

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("mfa@docflow.cl", "password"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<MfaRequiredException>();
        ex.Which.MfaToken.Should().Be("mfa-token-xyz");
        _totpServiceMock.Verify(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMfaEnabledAndInvalidMfaCode_ThrowsUnauthorized()
    {
        var usuario = AuthUserFactory.CreateUser("MFA User", "mfa@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa("JBSWY3DPEHPK3PXP");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("mfa@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("password", "$2b$hash")).Returns(true);
        _totpServiceMock.Setup(x => x.ValidateCode("JBSWY3DPEHPK3PXP", "000000")).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginCommand("mfa@docflow.cl", "password", "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Código de verificación inválido.");
    }

    [Fact]
    public async Task Handle_WithoutMfa_MfaCodeParameterIsIgnored()
    {
        var usuario = AuthUserFactory.CreateUser("No MFA", "no-mfa@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");

        _usuarioRepositoryMock.Setup(x => x.GetByIdentifierAsync("no-mfa@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Verify("password", "$2b$hash")).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>())).Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("10.0.0.1");

        var sut = CreateSut();

        var result = await sut.Handle(new LoginCommand("no-mfa@docflow.cl", "password", "123456"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        _totpServiceMock.Verify(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
