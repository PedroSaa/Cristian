using DocFlow.Application.Auth.Commands.Login;
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

public class LoginMfaCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private LoginMfaCommandHandler CreateSut() =>
        new(_usuarioRepositoryMock.Object, _jwtProviderMock.Object, _totpServiceMock.Object, _passwordHasherMock.Object, _mediatorMock.Object, _currentUserMock.Object, AuthUserFactory.PassthroughMfaProtector());

    [Fact]
    public async Task Handle_WithValidMfaTokenAndCode_ReturnsLoginResult()
    {
        var userId = Guid.NewGuid();
        var mfaToken = "valid-mfa-token";
        var code = "123456";
        var secret = "JBSWY3DPEHPK3PXP";
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa(secret);

        _jwtProviderMock.Setup(x => x.ValidateMfaToken(mfaToken)).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.ValidateCode(secret, code)).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>())).Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("10.0.0.1");

        var sut = CreateSut();

        var result = await sut.Handle(new LoginMfaCommand(mfaToken, code), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_PublishesEventWithRealIp()
    {
        var userId = Guid.NewGuid();
        var mfaToken = "valid-mfa-token";
        var code = "123456";
        var secret = "JBSWY3DPEHPK3PXP";
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa(secret);

        _jwtProviderMock.Setup(x => x.ValidateMfaToken(mfaToken)).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.ValidateCode(secret, code)).Returns(true);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>())).Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("192.168.1.200");

        var sut = CreateSut();

        await sut.Handle(new LoginMfaCommand(mfaToken, code), CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(It.Is<UsuarioAutenticadoEvent>(e => e.Ip == "192.168.1.200" && e.Metodo == "mfa"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var mfaToken = "valid-mfa-token";
        var code = "000000";
        var secret = "JBSWY3DPEHPK3PXP";
        var usuario = AuthUserFactory.CreateUser("Test User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "$2b$hash");
        usuario.EstablecerMfa(secret);

        _jwtProviderMock.Setup(x => x.ValidateMfaToken(mfaToken)).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _totpServiceMock.Setup(x => x.ValidateCode(secret, code)).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginMfaCommand(mfaToken, code), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*código*");
    }

    [Fact]
    public async Task Handle_WithInvalidMfaToken_ThrowsUnauthorized()
    {
        _jwtProviderMock.Setup(x => x.ValidateMfaToken("invalid-token")).Returns((Guid?)null);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginMfaCommand("invalid-token", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _usuarioRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();

        _jwtProviderMock.Setup(x => x.ValidateMfaToken("valid-token")).Returns(userId);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        var act = () => sut.Handle(new LoginMfaCommand("valid-token", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
