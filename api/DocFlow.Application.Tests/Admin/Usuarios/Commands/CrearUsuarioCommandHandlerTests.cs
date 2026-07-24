using DocFlow.Application.Admin.Usuarios.Commands.CrearUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands;

public class CrearUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<CrearUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<IRolRepository> _rolRepoMock = new();

    private readonly Guid _adminId = Guid.NewGuid();

    private CrearUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        return new(_repoMock.Object, _auditoriaMock.Object, _passwordHasherMock.Object, _currentUserMock.Object, _loggerMock.Object, _rolRepoMock.Object);
    }

    [Fact]
    public async Task Handle_CreaUsuarioConPersonalYCuenta()
    {
        var rolId = Guid.NewGuid();
        _repoMock.Setup(x => x.ExistsByCorreoAsync("user@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(x => x.ExistsByRutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(x => x.ExistsByUsucodAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.Hash("Secure@123")).Returns("$2b$hashed-password");
        _rolRepoMock.Setup(x => x.GetByNombreAsync("Usuario")).ReturnsAsync(new Rol(rolId, "Usuario", "Usuario del sistema"));

        var sut = CreateSut();

        var result = await sut.Handle(new CrearUsuarioCommand("Test", "User", "", null, null, "user@docflow.cl", "Usuario", null, "Secure@123", Usucod: "user1"), CancellationToken.None);

        result.Email.Should().Be("user@docflow.cl");
        result.Usucod.Should().Be("user1");
        _repoMock.Verify(x => x.CreateAsync(
            It.Is<SePersonal>(p =>
                p.Correo == "user@docflow.cl" &&
                p.Nombres == "Test" &&
                p.ApellidoPaterno == "User"),
            It.Is<SeUsuari>(u => u.Usucod == "user1" && u.RolId == rolId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConCorreoDuplicado_LanzaError()
    {
        _repoMock.Setup(x => x.ExistsByCorreoAsync("dup@docflow.cl", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        var act = () => sut.Handle(new CrearUsuarioCommand("Duplicate", "", "", null, null, "dup@docflow.cl", "Usuario", null, "Secure@123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ya existe un usuario con el correo dup@docflow.cl.");
    }

    [Fact]
    public async Task Handle_SinUsuarioActual_LanzaUnauthorizedYNoEscribe()
    {
        _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);
        var sut = new CrearUsuarioCommandHandler(_repoMock.Object, _auditoriaMock.Object, _passwordHasherMock.Object, _currentUserMock.Object, _loggerMock.Object, _rolRepoMock.Object);

        var act = () => sut.Handle(new CrearUsuarioCommand("Test", "User", "", null, null, "user@docflow.cl", "Usuario", null, "Secure@123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _repoMock.Verify(x => x.CreateAsync(It.IsAny<SePersonal>(), It.IsAny<SeUsuari>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    private static CrearUsuarioCommandValidator CreateValidator()
    {
        var policyMock = new Mock<ISecurityPolicyService>();
        policyMock.Setup(p => p.GetPasswordMinLength()).Returns(1);
        policyMock.Setup(p => p.GetPasswordRequireUpper()).Returns(false);
        policyMock.Setup(p => p.GetPasswordRequireSpecial()).Returns(false);
        return new CrearUsuarioCommandValidator(policyMock.Object);
    }

    [Fact]
    public void Validate_ConRutMayorA20_Falla()
    {
        var validator = CreateValidator();
        var cmd = new CrearUsuarioCommand("Test", "User", "", null, null, "user@docflow.cl", "Usuario", null, "secret", Rut: new string('1', 21));

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rut");
    }

    [Fact]
    public void Validate_ConRutDe20_Pasa()
    {
        var validator = CreateValidator();
        var cmd = new CrearUsuarioCommand("Test", "User", "", null, null, "user@docflow.cl", "Usuario", null, "secret", Rut: new string('1', 20));

        var result = validator.Validate(cmd);

        result.Errors.Should().NotContain(e => e.PropertyName == "Rut");
    }
}
