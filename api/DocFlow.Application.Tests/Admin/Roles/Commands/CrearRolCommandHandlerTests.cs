using DocFlow.Application.Admin.Roles.Commands.CrearRol;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Roles.Commands;

public class CrearRolCommandHandlerTests
{
    private readonly Mock<IRolRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private CrearRolCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        return new CrearRolCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndReturnsRol()
    {
        // Arrange
        _repoMock.Setup(x => x.ExistsByNombreAsync("Admin")).ReturnsAsync(false);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new CrearRolCommand("Admin", "Administrador del sistema"), CancellationToken.None);

        // Assert
        result.Nombre.Should().Be("Admin");
        result.Descripcion.Should().Be("Administrador del sistema");
        _repoMock.Verify(x => x.CreateAsync(It.Is<Rol>(r => r.Nombre == "Admin")), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "RolCreado" &&
            r.Entidad == "Rol" &&
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateNombre_ThrowsInvalidOperationException()
    {
        // Arrange
        _repoMock.Setup(x => x.ExistsByNombreAsync("Admin")).ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new CrearRolCommand("Admin", null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un rol con el nombre Admin.");
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutCurrentUser_ThrowsUnauthorizedAndDoesNotWrite()
    {
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);
        var sut = new CrearRolCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);

        var act = () => sut.Handle(new CrearRolCommand("Admin", "Administrador"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repoMock.Verify(x => x.CreateAsync(It.IsAny<Rol>()), Times.Never);
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutDescripcion_CreatesWithNullDescripcion()
    {
        // Arrange
        _repoMock.Setup(x => x.ExistsByNombreAsync("User")).ReturnsAsync(false);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new CrearRolCommand("User", null), CancellationToken.None);

        // Assert
        result.Nombre.Should().Be("User");
        result.Descripcion.Should().BeNull();
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Once);
    }

    [Fact]
    public void Validate_WithEmptyNombre_Fails()
    {
        // Arrange
        var validator = new CrearRolCommandValidator();
        var cmd = new CrearRolCommand("", "desc");

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Validate_WithTooLongNombre_Fails()
    {
        // Arrange
        var validator = new CrearRolCommandValidator();
        var cmd = new CrearRolCommand(new string('x', 101), "desc");

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidData_Passes()
    {
        // Arrange
        var validator = new CrearRolCommandValidator();
        var cmd = new CrearRolCommand("Admin", "Administrador");

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooLongDescripcion_Fails()
    {
        // Arrange
        var validator = new CrearRolCommandValidator();
        var cmd = new CrearRolCommand("Admin", new string('x', 501));

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }

    [Fact]
    public void Validate_WithDescripcionOf500_Passes()
    {
        // Arrange
        var validator = new CrearRolCommandValidator();
        var cmd = new CrearRolCommand("Admin", new string('x', 500));

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
