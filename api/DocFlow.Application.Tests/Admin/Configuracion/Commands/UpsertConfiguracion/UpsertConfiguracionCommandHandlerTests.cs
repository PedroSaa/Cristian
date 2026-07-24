using DocFlow.Application.Admin.Configuracion.Commands.UpsertConfiguracion;
using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UpsertConfiguracion;

public class UpsertConfiguracionCommandHandlerTests
{
    private readonly Mock<IConfiguracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<UpsertConfiguracionCommandHandler>> _loggerMock = new();
    private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new(MockBehavior.Strict);
    private readonly UpsertConfiguracionCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public UpsertConfiguracionCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _securityPolicyMock.Setup(s => s.Invalidate(It.IsAny<string>()));
        _handler = new UpsertConfiguracionCommandHandler(_repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object, _securityPolicyMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Create_New_Entry_When_Clave_Does_Not_Exist()
    {
        var cmd = new UpsertConfiguracionCommand("MAX_INTENTOS", "5", "Intentos máximos de login");

        _repoMock.Setup(r => r.GetByClaveAsync("MAX_INTENTOS")).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Clave.Should().Be("MAX_INTENTOS");
        result.Valor.Should().Be("5");
        result.Descripcion.Should().Be("Intentos máximos de login");
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Update_Existing_Entry_When_Clave_Exists()
    {
        var existing = ConfiguracionSistema.Crear(Guid.NewGuid(), "MAX_INTENTOS", "3", "Intentos antiguos");
        var cmd = new UpsertConfiguracionCommand("MAX_INTENTOS", "5", null); // Descripcion null => keep existing

        _repoMock.Setup(r => r.GetByClaveAsync("MAX_INTENTOS")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Clave.Should().Be("MAX_INTENTOS");
        result.Valor.Should().Be("5");               // updated
        result.Descripcion.Should().Be("Intentos antiguos"); // kept from existing
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    // ---- Cache invalidation (Slice 4) ----

    [Fact]
    public async Task Should_Invalidate_Cache_When_Upserting_Security_Key()
    {
        var cmd = new UpsertConfiguracionCommand("LockoutMaxIntentos", "5", "Max intentos");

        _repoMock.Setup(r => r.GetByClaveAsync("LockoutMaxIntentos")).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        await _handler.Handle(cmd, CancellationToken.None);

        _securityPolicyMock.Verify(s => s.Invalidate("LockoutMaxIntentos"), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Invalidate_Cache_When_Upserting_Non_Security_Key()
    {
        var cmd = new UpsertConfiguracionCommand("NombreInstitucion", "Mi Instituto", "Nombre");

        _repoMock.Setup(r => r.GetByClaveAsync("NombreInstitucion")).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        await _handler.Handle(cmd, CancellationToken.None);

        _securityPolicyMock.Verify(s => s.Invalidate(It.IsAny<string>()), Times.Never);
    }
}
