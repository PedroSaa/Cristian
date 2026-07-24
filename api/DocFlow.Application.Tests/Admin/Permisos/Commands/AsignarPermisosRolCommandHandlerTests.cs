using DocFlow.Application.Admin.Permisos.Commands;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Permisos.Commands;

public class AsignarPermisosRolCommandHandlerTests
{
    private readonly Mock<IRolRepository> _rolRepoMock = new();
    private readonly Mock<IPermisoRepository> _permisoRepoMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private AsignarPermisosRolCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        return new(
            _rolRepoMock.Object,
            _permisoRepoMock.Object,
            _permissionServiceMock.Object,
            _auditoriaMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidIds_ReplacesPermissionsAtomically()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        var permisoId1 = Guid.NewGuid();
        var permisoId2 = Guid.NewGuid();
        var rol = new Rol(rolId, "Supervisor", null);

        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(rol);
        _permisoRepoMock.Setup(x => x.GetByRolIdAsync(rolId)).ReturnsAsync(Array.Empty<Permiso>());
        _permisoRepoMock.Setup(x => x.GetByIdAsync(permisoId1)).ReturnsAsync(new Permiso(permisoId1, "p1", null, "g1"));
        _permisoRepoMock.Setup(x => x.GetByIdAsync(permisoId2)).ReturnsAsync(new Permiso(permisoId2, "p2", null, "g1"));
        _permissionServiceMock.Setup(x => x.InvalidateAllAsync()).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var command = new AsignarPermisosRolCommand(rolId, new[] { permisoId1, permisoId2 });

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _permisoRepoMock.Verify(x => x.UpdateRolPermisosAsync(rolId, new[] { permisoId1, permisoId2 }), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidIds_InvalidatesPermissionCacheImmediately()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        var permisoId = Guid.NewGuid();

        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol(rolId, "Supervisor", null));
        _permisoRepoMock.Setup(x => x.GetByRolIdAsync(rolId)).ReturnsAsync(Array.Empty<Permiso>());
        _permisoRepoMock.Setup(x => x.GetByIdAsync(permisoId)).ReturnsAsync(new Permiso(permisoId, "p1", null, "g1"));
        _permissionServiceMock.Setup(x => x.InvalidateAllAsync()).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.Handle(new AsignarPermisosRolCommand(rolId, new[] { permisoId }), CancellationToken.None);

        // Assert
        _permissionServiceMock.Verify(x => x.InvalidateAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidIds_CreatesAuditEntryWithActorAndBeforeAfterDetail()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        var permisoAnterior = new Permiso(Guid.NewGuid(), "documentos.ver", null, "documentos");
        var permisoNuevo = new Permiso(Guid.NewGuid(), "documentos.crear", null, "documentos");

        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol(rolId, "Supervisor", null));
        _permisoRepoMock.Setup(x => x.GetByRolIdAsync(rolId)).ReturnsAsync(new[] { permisoAnterior });
        _permisoRepoMock.Setup(x => x.GetByIdAsync(permisoNuevo.Id)).ReturnsAsync(permisoNuevo);
        _permissionServiceMock.Setup(x => x.InvalidateAllAsync()).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.Handle(new AsignarPermisosRolCommand(rolId, new[] { permisoNuevo.Id }), CancellationToken.None);

        // Assert
        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId &&
            r.Accion == "PermisosRolAsignados" &&
            r.Entidad == "Rol" &&
            r.EntidadId == rolId.ToString() &&
            r.Detalle.Contains("documentos.ver") &&
            r.Detalle.Contains("documentos.crear"))), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUser_ThrowsUnauthorizedAndDoesNotMutateOrAudit()
    {
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);
        var sut = new AsignarPermisosRolCommandHandler(
            _rolRepoMock.Object,
            _permisoRepoMock.Object,
            _permissionServiceMock.Object,
            _auditoriaMock.Object,
            _currentUserMock.Object);

        var act = () => sut.Handle(new AsignarPermisosRolCommand(Guid.NewGuid(), new[] { Guid.NewGuid() }), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _permisoRepoMock.Verify(x => x.UpdateRolPermisosAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _permissionServiceMock.Verify(x => x.InvalidateAllAsync(), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyPermisoIds_ClearsAllPermissions()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol(rolId, "Supervisor", null));
        _permisoRepoMock.Setup(x => x.GetByRolIdAsync(rolId)).ReturnsAsync(Array.Empty<Permiso>());
        _permissionServiceMock.Setup(x => x.InvalidateAllAsync()).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var command = new AsignarPermisosRolCommand(rolId, Array.Empty<Guid>());

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _permisoRepoMock.Verify(x => x.UpdateRolPermisosAsync(rolId, It.Is<IEnumerable<Guid>>(e => !e.Any())), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownPermisoId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        var unknownId = Guid.NewGuid();
        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync(new Rol(rolId, "Supervisor", null));
        _permisoRepoMock.Setup(x => x.GetByRolIdAsync(rolId)).ReturnsAsync(Array.Empty<Permiso>());
        _permisoRepoMock.Setup(x => x.GetByIdAsync(unknownId)).ReturnsAsync((Permiso?)null);

        var sut = CreateSut();
        var command = new AsignarPermisosRolCommand(rolId, new[] { unknownId });

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{unknownId}*");
        _permisoRepoMock.Verify(x => x.UpdateRolPermisosAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _permissionServiceMock.Verify(x => x.InvalidateAllAsync(), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentRol_ThrowsKeyNotFoundException()
    {
        // Arrange
        var rolId = Guid.NewGuid();
        _rolRepoMock.Setup(x => x.GetByIdAsync(rolId)).ReturnsAsync((Rol?)null);

        var sut = CreateSut();
        var command = new AsignarPermisosRolCommand(rolId, new[] { Guid.NewGuid() });

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{rolId}*");
        _permisoRepoMock.Verify(x => x.UpdateRolPermisosAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _permissionServiceMock.Verify(x => x.InvalidateAllAsync(), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }
}

public class AsignarPermisosRolCommandValidatorTests
{
    private readonly AsignarPermisosRolCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyRolId_Fails()
    {
        // Arrange
        var command = new AsignarPermisosRolCommand(Guid.Empty, new[] { Guid.NewGuid() });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RolId");
    }

    [Fact]
    public void Validate_WithNullPermisoIds_Fails()
    {
        // Arrange
        var command = new AsignarPermisosRolCommand(Guid.NewGuid(), null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PermisoIds");
    }

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        // Arrange
        var command = new AsignarPermisosRolCommand(Guid.NewGuid(), new[] { Guid.NewGuid() });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
