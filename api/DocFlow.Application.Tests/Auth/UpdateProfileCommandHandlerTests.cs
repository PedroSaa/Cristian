using DocFlow.Application.Auth.Commands.UpdateProfile;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISeUsuariRepository> _usuarioRepositoryMock = new();
    private readonly Mock<ISePersonalRepository> _personalRepositoryMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();

    private UpdateProfileHandler CreateSut() =>
        new(_currentUserMock.Object, _usuarioRepositoryMock.Object, _personalRepositoryMock.Object, _auditoriaMock.Object);

    [Fact]
    public async Task Handle_WithNombreAndEmail_ReturnsCanonicalUpdatedUser()
    {
        var usuario = AuthUserFactory.CreateUser("Nombre Inicial", "inicial@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "stored-hash");

        _currentUserMock.SetupGet(x => x.UserId).Returns(usuario.Id);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sut = CreateSut();

        var result = await sut.Handle(new UpdateProfileCommand("Nombre Final", "final@docflow.cl"), CancellationToken.None);

        result.Nombre.Should().Be("Nombre Final");
        result.Email.Should().Be("final@docflow.cl");
        _personalRepositoryMock.Verify(x => x.UpdateAsync(usuario.Personal!, It.IsAny<CancellationToken>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "ActualizarPerfil" && r.Detalle.Contains("final@docflow.cl"))), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmailUsedByAnotherUser_ThrowsAndDoesNotUpdate()
    {
        var usuario = AuthUserFactory.CreateUser("Nombre", "inicial@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "stored-hash");

        _currentUserMock.SetupGet(x => x.UserId).Returns(usuario.Id);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        // El correo ya pertenece a OTRA persona (usucod distinto).
        _personalRepositoryMock.Setup(x => x.GetByCorreoAsync("ocupado@docflow.cl", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SePersonal.Crear("otro-usucod", "Otro Usuario", correo: "ocupado@docflow.cl"));

        var act = () => CreateSut().Handle(new UpdateProfileCommand("Nombre", "ocupado@docflow.cl"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ocupado@docflow.cl*");
        _personalRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<SePersonal>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoriaMock.Verify(a => a.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KeepingSameEmail_DoesNotCheckUniqueness()
    {
        var usuario = AuthUserFactory.CreateUser("Nombre", "inicial@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), passwordHash: "stored-hash");

        _currentUserMock.SetupGet(x => x.UserId).Returns(usuario.Id);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(true);
        _usuarioRepositoryMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        // Mismo email que el actual: no debe consultarse unicidad (no es un cambio).
        await CreateSut().Handle(new UpdateProfileCommand("Nombre Nuevo", "inicial@docflow.cl"), CancellationToken.None);

        _personalRepositoryMock.Verify(x => x.GetByCorreoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _personalRepositoryMock.Verify(x => x.UpdateAsync(usuario.Personal!, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutAuthenticatedUser_ThrowsUnauthorized()
    {
        _currentUserMock.SetupGet(x => x.UserId).Returns((Guid?)null);
        _currentUserMock.SetupGet(x => x.IsAuthenticated).Returns(false);

        var sut = CreateSut();

        var act = () => sut.Handle(new UpdateProfileCommand("Nombre Final", null), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
