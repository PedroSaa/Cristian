using DocFlow.Application.Admin.Auditoria.Services;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.Services;

public class AuditoriaServiceTests
{
    private readonly Mock<IAuditoriaRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpContextAccessor> _httpMock = new(MockBehavior.Strict);
    private readonly AuditoriaService _service;

    public AuditoriaServiceTests()
    {
        _service = new AuditoriaService(_repoMock.Object, _httpMock.Object);
    }

    [Fact]
    public async Task RegistrarAsync_WithHttpContext_Should_CaptureIpAndUserAgent()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var ip = "192.168.1.100";
        var userAgent = "Mozilla/5.0 Chrome/120";

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        httpContext.Request.Headers["User-Agent"] = userAgent;

        _httpMock.Setup(x => x.HttpContext).Returns(httpContext);

        RegistroAuditoria? captured = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegistrarAsync(usuarioId, "Login", "Usuario", "usr-1", "Inicio de sesión");

        // Assert
        captured.Should().NotBeNull();
        captured!.DireccionIp.Should().Be(ip);
        captured.UserAgent.Should().Be(userAgent);
        captured.UsuarioId.Should().Be(usuarioId);
        captured.Accion.Should().Be("Login");
        captured.Entidad.Should().Be("Usuario");
        captured.EntidadId.Should().Be("usr-1");
        captured.Detalle.Should().Be("Inicio de sesión");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_WithoutHttpContext_Should_SetNullIpAndUserAgent()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();

        _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        RegistroAuditoria? captured = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegistrarAsync(usuarioId, "Logout", "Usuario", "usr-2", "Cierre de sesión");

        // Assert
        captured.Should().NotBeNull();
        captured!.DireccionIp.Should().BeNull();
        captured.UserAgent.Should().BeNull();
    }

    [Fact]
    public async Task RegistrarAsync_WithHttpContextNoHeaders_Should_SetNullUserAgent()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        // No User-Agent header

        _httpMock.Setup(x => x.HttpContext).Returns(httpContext);

        RegistroAuditoria? captured = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => captured = r)
            .Returns(Task.CompletedTask);

        // Act
        await _service.RegistrarAsync(usuarioId, "Update", "Config", "cfg-1", "Actualizado");

        // Assert
        captured.Should().NotBeNull();
        captured!.DireccionIp.Should().Be("10.0.0.1");
        captured.UserAgent.Should().BeNull();
    }
}
