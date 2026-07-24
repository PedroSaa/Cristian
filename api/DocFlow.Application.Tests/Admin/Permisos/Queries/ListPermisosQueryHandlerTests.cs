using DocFlow.Application.Admin.Permisos.DTOs;
using DocFlow.Application.Admin.Permisos.Queries;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Permisos.Queries;

public class ListPermisosQueryHandlerTests
{
    private readonly Mock<IPermisoRepository> _repoMock = new();

    private ListPermisosQueryHandler CreateSut() => new(_repoMock.Object);

    [Fact]
    public async Task Handle_WhenPermissionsExist_ReturnsAllPermissions()
    {
        // Arrange
        var permisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "bandeja.ver", "Ver bandeja", "bandeja"),
            new(Guid.NewGuid(), "documentos.crear", "Crear documentos", "documentos"),
            new(Guid.NewGuid(), "admin.usuarios.ver", "Ver usuarios", "admin")
        };
        _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(permisos);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ListPermisosQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Nombre == "bandeja.ver" && p.Grupo == "bandeja");
        result.Should().Contain(p => p.Nombre == "documentos.crear" && p.Grupo == "documentos");
        result.Should().Contain(p => p.Nombre == "admin.usuarios.ver" && p.Grupo == "admin");
    }

    [Fact]
    public async Task Handle_WhenNoPermissions_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Permiso>());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ListPermisosQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsPermissionsWithCorrectMapping()
    {
        // Arrange
        var permiso = new Permiso(
            Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            "reportes.generar",
            "Generar reportes del sistema",
            "reportes");
        _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new[] { permiso });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ListPermisosQuery(), CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(permiso.Id);
        dto.Nombre.Should().Be("reportes.generar");
        dto.Descripcion.Should().Be("Generar reportes del sistema");
        dto.Grupo.Should().Be("reportes");
    }
}
