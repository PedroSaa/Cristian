using DocFlow.Application.Auth.Commands.RefreshToken;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    private RefreshTokenCommandHandler CreateSut() =>
        new(_jwtProviderMock.Object, _usuarioRepositoryMock.Object, _mediatorMock.Object, _securityPolicyMock.Object);

    [Fact]
    public async Task Handle_WithStoredRefreshToken_RotatesTokensAndReturnsCanonicalUser()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Grace Hopper",
            "grace@docflow.cl",
            nameof(RolUsuario.Usuario),
            AuthUserFactory.UsuarioPermissions(),
            passwordHash: "stored-hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>()))
            .Returns(("new-access", "new-refresh", DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();

        var result = await sut.Handle(new RefreshTokenCommand("refresh-token"), CancellationToken.None);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        result.ExpiresIn.Should().BeGreaterThan(0);
        result.User.Id.Should().Be(usuario.Id);
        result.User.Nombre.Should().Be("Grace Hopper");
        result.User.Email.Should().Be("grace@docflow.cl");
        result.User.Rol.Should().Be(nameof(RolUsuario.Usuario));
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
        _jwtProviderMock.Verify(x => x.GenerateTokens(usuario, usuario.Personal!, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPolicyChangesToMandatoryMfa_ReturnsSetupOnlyStateWithoutRotating()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Grace Hopper",
            "grace@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "stored-hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForAdministrators()).Returns(true);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForOtherUsers()).Returns(false);
        _jwtProviderMock.Setup(x => x.GenerateMfaToken(usuario.UsuarioId)).Returns("setup-token");

        var sut = CreateSut();

        var result = await sut.Handle(new RefreshTokenCommand("refresh-token"), CancellationToken.None);

        result.AuthState.Should().Be(AuthState.MfaSetupRequired);
        result.SetupToken.Should().Be("setup-token");
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingStoredSession_RejectsRefreshWithoutRotating()
    {
        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        var act = () => sut.Handle(new RefreshTokenCommand("refresh-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("La sesión no es válida.");
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredRefreshToken_ReturnsUnauthorizedWithoutMintingTokens()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Grace Hopper",
            "grace@docflow.cl",
            nameof(RolUsuario.Usuario),
            AuthUserFactory.UsuarioPermissions(),
            passwordHash: "stored-hash");
        usuario.SetRefreshToken("expired-token", DateTime.UtcNow.AddMinutes(-1));

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("expired-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        var act = () => sut.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("La sesión expiró.");
        _jwtProviderMock.Verify(x => x.GenerateTokens(It.IsAny<SeUsuari>(), It.IsAny<SePersonal>(), It.IsAny<bool>()), Times.Never);
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
