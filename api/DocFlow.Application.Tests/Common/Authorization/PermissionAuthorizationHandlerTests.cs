using System.Security.Claims;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;
using FluentAssertions;

namespace DocFlow.Application.Tests.Common.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly PermissionAuthorizationHandler _handler;

    public PermissionAuthorizationHandlerTests()
    {
        _handler = new PermissionAuthorizationHandler(_permissionServiceMock.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserHasPermission_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        var requirement = new PermissionRequirement("admin.usuarios.ver");
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, user, null);

        _permissionServiceMock
            .Setup(x => x.UserHasPermissionAsync(userId, "admin.usuarios.ver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_UserLacksPermission_DoesNotSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        var requirement = new PermissionRequirement("admin.usuarios.ver");
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, user, null);

        _permissionServiceMock
            .Setup(x => x.UserHasPermissionAsync(userId, "admin.usuarios.ver", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_UserWithoutNameIdentifier_DoesNotSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var requirement = new PermissionRequirement("admin.usuarios.ver");
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
        _permissionServiceMock.Verify(
            x => x.UserHasPermissionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
