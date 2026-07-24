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

public class LoginMfaCommandHandlerMfaClaimTests
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
    public async Task Handle_WithValidCredentials_CallsGenerateTokensWithMfaCompletedTrue()
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
        _jwtProviderMock.Setup(x => x.GenerateTokens(usuario, usuario.Personal!, true))
            .Returns(("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30)));

        var sut = CreateSut();

        await sut.Handle(new LoginMfaCommand(mfaToken, code), CancellationToken.None);

        _jwtProviderMock.Verify(x => x.GenerateTokens(usuario, usuario.Personal!, true), Times.Once);
    }
}
