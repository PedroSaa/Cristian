using DocFlow.Application.Admin.Roles.Queries.ListRoles;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Queries;

public class ListRolesQueryHandlerTests
{
    private readonly Mock<IRolRepository> _repoMock = new();

    private ListRolesQueryHandler CreateSut() => new(_repoMock.Object);

    [Fact]
    public async Task Handle_WithRoles_ReturnsMappedDtos()
    {
        // Arrange
        var roles = new List<Rol>
        {
            new Rol(Guid.NewGuid(), "Admin", "Administrador", esSistema: true),
            new Rol(Guid.NewGuid(), "User", "Usuario regular", esSistema: false)
        };
        _repoMock.Setup(x => x.GetAllWithPermisosAsync()).ReturnsAsync(roles);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ListRolesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Nombre.Should().Be("Admin");
        result[0].EsSistema.Should().BeTrue();
        result[1].Nombre.Should().Be("User");
        result[1].EsSistema.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.GetAllWithPermisosAsync()).ReturnsAsync(new List<Rol>());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ListRolesQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
