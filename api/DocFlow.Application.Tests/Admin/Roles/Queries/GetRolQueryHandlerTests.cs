using DocFlow.Application.Admin.Permisos.DTOs;
using DocFlow.Application.Admin.Roles.DTOs;
using DocFlow.Application.Admin.Roles.Queries.GetRol;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Queries;

public class GetRolQueryHandlerTests
{
    private readonly Mock<IRolRepository> _repoMock = new();

    private GetRolQueryHandler CreateSut() => new(_repoMock.Object);

    [Fact]
    public async Task Handle_WithExistingRole_ReturnsRolDto()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Rol(roleId, "Administrador", "Rol administrador", esSistema: true);
        _repoMock.Setup(x => x.GetByIdWithPermisosAsync(roleId)).ReturnsAsync(role);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetRolQuery(roleId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
        result.Nombre.Should().Be("Administrador");
        result.Descripcion.Should().Be("Rol administrador");
        result.EsSistema.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentRole_ThrowsKeyNotFoundException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithPermisosAsync(roleId)).ReturnsAsync((Rol?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new GetRolQuery(roleId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{roleId}*");
    }

    [Fact]
    public async Task Handle_WithPermisos_ReturnsRolDtoWithPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permisoId1 = Guid.NewGuid();
        var permisoId2 = Guid.NewGuid();

        var permiso1 = new Permiso(permisoId1, "bandeja.ver", "Ver bandeja", "bandeja");
        var permiso2 = new Permiso(permisoId2, "documentos.crear", "Crear documentos", "documentos");

        var role = new Rol(roleId, "Supervisor", "Rol supervisor", esSistema: false);
        role.RolPermisos.Add(new RolPermiso(roleId, permisoId1) { Permiso = permiso1 });
        role.RolPermisos.Add(new RolPermiso(roleId, permisoId2) { Permiso = permiso2 });

        _repoMock.Setup(x => x.GetByIdWithPermisosAsync(roleId)).ReturnsAsync(role);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetRolQuery(roleId), CancellationToken.None);

        // Assert
        result.Permisos.Should().NotBeNull();
        result.Permisos.Should().HaveCount(2);
        result.Permisos.Should().Contain(p => p.Nombre == "bandeja.ver" && p.Grupo == "bandeja");
        result.Permisos.Should().Contain(p => p.Nombre == "documentos.crear" && p.Grupo == "documentos");
    }

    [Fact]
    public async Task Handle_WithoutPermisos_ReturnsRolDtoWithNullPermisos()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new Rol(roleId, "Admin", "Admin role", esSistema: true);
        _repoMock.Setup(x => x.GetByIdWithPermisosAsync(roleId)).ReturnsAsync(role);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetRolQuery(roleId), CancellationToken.None);

        // Assert
        result.Permisos.Should().BeNull();
    }
}
