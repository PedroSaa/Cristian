using DocFlow.Application.Auth.Commands.Logout;
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

public class LogoutCommandHandlerTests
{
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private LogoutCommandHandler CreateSut() =>
        new(_usuarioRepositoryMock.Object, _mediatorMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WithValidToken_ClearsRefreshTokenAndPublishesEventWithRealIp()
    {
        var usuario = AuthUserFactory.CreateUser("Test User", "test@docflow.cl", nameof(RolUsuario.Administrador), AuthUserFactory.AdminPermissions(), passwordHash: "$2b$hash");
        usuario.SetRefreshToken("valid-refresh-token", DateTime.UtcNow.AddDays(1));

        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("valid-refresh-token", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _currentUserMock.SetupGet(x => x.IpAddress).Returns("10.0.0.1");

        var sut = CreateSut();

        await sut.Handle(new LogoutCommand("valid-refresh-token"), CancellationToken.None);

        usuario.AuthSessionVersion.Should().Be(1);
        usuario.RefreshTokenHash.Should().BeNull();
        _mediatorMock.Verify(x => x.Publish(It.Is<SesionCerradaEvent>(e => e.Ip == "10.0.0.1"), It.IsAny<CancellationToken>()), Times.Once);
        _usuarioRepositoryMock.Verify(x => x.UpdateAsync(usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsUnitWithoutPublishing()
    {
        _usuarioRepositoryMock.Setup(x => x.GetByRefreshTokenAsync("invalid-token", It.IsAny<CancellationToken>())).ReturnsAsync((SeUsuari?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new LogoutCommand("invalid-token"), CancellationToken.None);

        result.Should().Be(Unit.Value);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
