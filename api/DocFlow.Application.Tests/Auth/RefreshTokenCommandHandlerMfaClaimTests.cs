using DocFlow.Application.Auth.Commands.RefreshToken;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class RefreshTokenCommandHandlerMfaClaimTests
{
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    private RefreshTokenCommandHandler CreateSut() =>
        new(_jwtProviderMock.Object, _usuarioRepositoryMock.Object, _mediatorMock.Object, _securityPolicyMock.Object);

    [Fact]
    public async Task Handle_WhenUserHasMfaEnabled_CallsGenerateTokensWithMfaCompletedTrue()
    {
        var usuario = AuthUserFactory.CreateUser("MFA User", "mfa@docflow.cl", nameof(RolUsuario.Administrador), AuthUserFactory.AdminPermissions(), passwordHash: "hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));
        usuario.EstablecerMfa("secret");

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, true)).Returns(("new-access", "new-refresh", DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();

        await sut.Handle(new RefreshTokenCommand("refresh-token"), CancellationToken.None);

        _jwtProviderMock.Verify(x => x.GenerateTokens(usuario, usuario.Personal!, true), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasMfaDisabled_CallsGenerateTokensWithMfaCompletedFalse()
    {
        var usuario = AuthUserFactory.CreateUser("Non-MFA User", "nonmfa@docflow.cl", nameof(RolUsuario.Administrador), AuthUserFactory.AdminPermissions(), passwordHash: "hash");
        usuario.SetRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(30));

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _jwtProviderMock.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, false)).Returns(("new-access", "new-refresh", DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();

        await sut.Handle(new RefreshTokenCommand("refresh-token"), CancellationToken.None);

        _jwtProviderMock.Verify(x => x.GenerateTokens(usuario, usuario.Personal!, false), Times.Once);
    }
}
