using DocFlow.Application.Auth.EventHandlers;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class LoginFallidoHandlerTests
{
    [Fact]
    public async Task Handle_Should_Audit_Failed_Login_With_Ip_And_UserAgent()
    {
        var auditoriaMock = new Mock<IAuditoriaRepository>(MockBehavior.Strict);
        RegistroAuditoria? saved = null;
        auditoriaMock
            .Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => saved = r)
            .Returns(Task.CompletedTask);
        var handler = new LoginFallidoHandler(auditoriaMock.Object);
        var userId = Guid.NewGuid();

        await handler.Handle(
            new LoginFallidoEvent(userId, "test@docflow.cl", "Contraseña incorrecta", "1.2.3.4", "ua-test", 3),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Accion.Should().Be("LoginFallido");
        saved.Entidad.Should().Be("Usuario");
        saved.UsuarioId.Should().Be(userId);
        saved.DireccionIp.Should().Be("1.2.3.4");
        saved.UserAgent.Should().Be("ua-test");
        saved.Detalle.Should().Contain("test@docflow.cl");
        saved.Detalle.Should().Contain("Contraseña incorrecta");
    }

    [Fact]
    public async Task Handle_Should_Audit_Unknown_Identifier_With_Empty_UserId()
    {
        var auditoriaMock = new Mock<IAuditoriaRepository>(MockBehavior.Strict);
        RegistroAuditoria? saved = null;
        auditoriaMock
            .Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => saved = r)
            .Returns(Task.CompletedTask);
        var handler = new LoginFallidoHandler(auditoriaMock.Object);

        await handler.Handle(
            new LoginFallidoEvent(Guid.Empty, "ghost@docflow.cl", "Identificador inexistente", "1.2.3.4", null, 0),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.UsuarioId.Should().Be(Guid.Empty);
        saved.Detalle.Should().Contain("ghost@docflow.cl");
    }
}
