using DocFlow.Application.Admin.Usuarios.Commands.ResetPasswordUsuario;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Usuarios.Commands.ResetPasswordUsuario;

public class ResetPasswordUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioAdminRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<ResetPasswordUsuarioCommandHandler>> _loggerMock = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private ResetPasswordUsuarioCommandHandler CreateSut()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        return new(_repoMock.Object, _auditoriaMock.Object, _passwordHasherMock.Object, _mediatorMock.Object, _currentUserMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ReseteaClaveYLimpiaSesion()
    {
        var usuario = CreateUser();
        _repoMock.Setup(x => x.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _passwordHasherMock.Setup(x => x.Hash("NewPass123!")).Returns("$2b$new-hash");

        var sut = CreateSut();

        await sut.Handle(new ResetPasswordUsuarioCommand(usuario.Id, "NewPass123!"), CancellationToken.None);

        usuario.RefreshTokenHash.Should().BeNull();
        _repoMock.Verify(x => x.UpdateAsync(usuario.Personal!, usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SeUsuari CreateUser()
    {
        var personal = SePersonal.Crear("testuser", "Test User", correo: "test@docflow.cl");
        var usuario = SeUsuari.Crear(Guid.NewGuid(), "testuser", "hash", null, null, estadoCuenta: true);
        usuario.VincularPersonal(personal);
        usuario.SetRefreshToken("old-refresh", DateTime.UtcNow.AddDays(1));
        return usuario;
    }
}
