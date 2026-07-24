using DocFlow.Application.Auth.EventHandlers;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class UsuarioAutenticadoHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private UsuarioAutenticadoHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_ShouldCreateRegistroAuditoriaWithCorrectDetails()
    {
        var usuarioId = Guid.NewGuid();
        var ip = "192.168.1.1";
        var metodo = "password";
        var evt = new UsuarioAutenticadoEvent(usuarioId, ip, metodo);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "Login" &&
            r.Entidad == "Usuario" &&
            r.EntidadId == usuarioId.ToString() &&
            r.Detalle.Contains(metodo) &&
            r.Detalle.Contains(ip)
        )), Times.Once);
    }
}

public class UsuarioBloqueadoHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private UsuarioBloqueadoHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_ShouldCreateRegistroAuditoriaWithLockoutDetails()
    {
        var usuarioId = Guid.NewGuid();
        var duracion = 30;
        var intentos = 5;
        var origen = "manual";
        var evt = new UsuarioBloqueadoEvent(usuarioId, duracion, intentos, origen);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "CuentaBloqueada" &&
            r.Entidad == "Usuario" &&
            r.EntidadId == usuarioId.ToString() &&
            r.Detalle.Contains(duracion.ToString()) &&
            r.Detalle.Contains(intentos.ToString()) &&
            r.Detalle.Contains(origen)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOrigenAuto_IncludesOrigenInDetalle()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new UsuarioBloqueadoEvent(usuarioId, 30, 3, "auto");

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Detalle.Contains("auto")
        )), Times.Once);
    }
}

public class MFAActivadoHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private MFAActivadoHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_WhenActivando_ShouldCreateRegistroAuditoriaWithActivacionAction()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new MFAActivadoEvent(usuarioId, "activar");

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "MFAActivado" &&
            r.Entidad == "Usuario" &&
            r.Detalle.Contains("activado")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDesactivando_ShouldCreateRegistroAuditoriaWithDesactivacionAction()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new MFAActivadoEvent(usuarioId, "desactivar");

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "MFAActivado" &&
            r.Detalle.Contains("desactivado")
        )), Times.Once);
    }
}

public class SesionCerradaHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private SesionCerradaHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_ShouldCreateRegistroAuditoriaWithSessionEndDetails()
    {
        var usuarioId = Guid.NewGuid();
        var ip = "10.0.0.1";
        var evt = new SesionCerradaEvent(usuarioId, ip);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "SesionCerrada" &&
            r.Entidad == "Usuario" &&
            r.Detalle.Contains(ip)
        )), Times.Once);
    }
}

public class RefreshTokenRotadoHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private RefreshTokenRotadoHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_WhenTokenExpirado_ShouldCreateRegistroAuditoriaWithExpiracionInfo()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new RefreshTokenRotadoEvent(usuarioId, true);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "RefreshTokenRotado" &&
            r.Entidad == "Usuario" &&
            r.Detalle.Contains("TokenExpirado=True")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenNoExpirado_ShouldCreateRegistroAuditoriaWithRotacionInfo()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new RefreshTokenRotadoEvent(usuarioId, false);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "RefreshTokenRotado" &&
            r.Detalle.Contains("TokenExpirado=False")
        )), Times.Once);
    }
}

public class PasswordCambiadoHandlerTests
{
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private PasswordCambiadoHandler CreateSut() => new(_auditoriaMock.Object);

    [Fact]
    public async Task Handle_WhenIniciadoPorAdmin_ShouldCreateRegistroAuditoriaWithAdminDetails()
    {
        var usuarioId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var evt = new PasswordCambiadoEvent(usuarioId, "admin", adminId);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "PasswordCambiado" &&
            r.Entidad == "Usuario" &&
            r.EntidadId == usuarioId.ToString() &&
            r.Detalle.Contains("admin") &&
            r.Detalle.Contains(adminId.ToString())
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIniciadoPorUsuario_ShouldCreateRegistroAuditoriaWithSelfChangeDetails()
    {
        var usuarioId = Guid.NewGuid();
        var evt = new PasswordCambiadoEvent(usuarioId, "usuario", null);

        var sut = CreateSut();
        await sut.Handle(evt, CancellationToken.None);

        _auditoriaMock.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == usuarioId &&
            r.Accion == "PasswordCambiado" &&
            r.Detalle.Contains("usuario") &&
            r.Detalle.Contains("propia")
        )), Times.Once);
    }
}
