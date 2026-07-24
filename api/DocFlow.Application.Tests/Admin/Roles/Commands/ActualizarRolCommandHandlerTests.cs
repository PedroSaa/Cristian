using DocFlow.Application.Admin.Roles.Commands.ActualizarRol;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Commands;

public class ActualizarRolCommandHandlerTests
{
    private readonly Mock<IRolRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private ActualizarRolCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        return new ActualizarRolCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesAndReturnsRol()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Rol(id, "Admin", "Old desc");
        _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(existing);
        _repoMock.Setup(x => x.ExistsByNombreAsync("SuperAdmin")).ReturnsAsync(false);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ActualizarRolCommand(id, "SuperAdmin", "New desc"), CancellationToken.None);

        // Assert
        result.Nombre.Should().Be("SuperAdmin");
        result.Descripcion.Should().Be("New desc");
        _repoMock.Verify(x => x.UpdateAsync(It.Is<Rol>(r => r.Nombre == "SuperAdmin")), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "RolActualizado" &&
            r.Entidad == "Rol" &&
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateNombre_ThrowsInvalidOperationException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Rol(id, "Admin", "desc");
        _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(existing);
        _repoMock.Setup(x => x.ExistsByNombreAsync("Other")).ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new ActualizarRolCommand(id, "Other", "desc"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un rol con el nombre Other.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Rol?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new ActualizarRolCommand(id, "Any", null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"No se encontró el rol con id {id}.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameNombreAsExisting_DoesNotThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new Rol(id, "Admin", "desc");
        _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(existing);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new ActualizarRolCommand(id, "Admin", "updated"), CancellationToken.None);

        // Assert
        result.Nombre.Should().Be("Admin");
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<Rol>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "RolActualizado")), Times.Once);
    }

    [Fact]
    public void Validate_WithTooLongDescripcion_Fails()
    {
        var validator = new ActualizarRolCommandValidator();
        var cmd = new ActualizarRolCommand(Guid.NewGuid(), "Admin", new string('x', 501));

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }

    [Fact]
    public void Validate_WithDescripcionOf500_Passes()
    {
        var validator = new ActualizarRolCommandValidator();
        var cmd = new ActualizarRolCommand(Guid.NewGuid(), "Admin", new string('x', 500));

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
