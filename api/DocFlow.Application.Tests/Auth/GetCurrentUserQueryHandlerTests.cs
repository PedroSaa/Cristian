using DocFlow.Application.Auth.Queries.GetCurrentUser;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();

    private GetCurrentUserQueryHandler CreateSut() =>
        new(_currentUserMock.Object, _usuarioRepositoryMock.Object, _securityPolicyMock.Object);

    [Fact]
    public async Task Handle_WhenPolicyRequiresMfa_ReturnsSetupRequiredState()
    {
        var usuario = AuthUserFactory.CreateUser(
            "Ada Lovelace",
            "ada@docflow.cl",
            nameof(RolUsuario.Administrador),
            AuthUserFactory.AdminPermissions(),
            passwordHash: "stored-hash");

        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.SetupGet(x => x.UserId).Returns(usuario.Id);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForAdministrators()).Returns(true);
        _securityPolicyMock.Setup(x => x.IsMfaRequiredForOtherUsers()).Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.AuthState.Should().Be(AuthState.MfaSetupRequired);
        result.SetupToken.Should().BeNullOrEmpty();
    }
}
