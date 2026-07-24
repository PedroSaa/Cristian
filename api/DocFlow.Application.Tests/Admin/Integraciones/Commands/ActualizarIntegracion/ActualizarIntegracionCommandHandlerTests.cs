using DocFlow.Application.Admin.Integraciones.Commands.ActualizarIntegracion;
using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Integraciones.Commands.ActualizarIntegracion;

public class ActualizarIntegracionCommandHandlerTests
{
    private readonly Mock<IIntegracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly Mock<IIntegracionConfigService> _integracionConfigMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ActualizarIntegracionCommandHandler>> _loggerMock = new();
    private readonly ActualizarIntegracionCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public ActualizarIntegracionCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(_adminId);
        _integracionConfigMock.Setup(c => c.Invalidate(It.IsAny<string>()));
        _handler = new ActualizarIntegracionCommandHandler(
            _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object,
            _integracionConfigMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Update_And_Return_Dto()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://old.url", "sk-1234567890abcdef", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfiguracionIntegracion>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarIntegracionCommand(id, "https://new.url", "new-key", false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeOfType<IntegracionDto>();
        result.BaseUrl.Should().Be("https://new.url");
        result.Activo.Should().BeFalse();

        integracion.BaseUrl.Should().Be("https://new.url");
        integracion.ApiKey.Should().Be("new-key");
        integracion.Activo.Should().BeFalse();

        _repoMock.Verify(r => r.UpdateAsync(integracion), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Keep_Existing_ApiKey_When_Null()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://old.url", "existing-key", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfiguracionIntegracion>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarIntegracionCommand(id, "https://new.url", null, true);
        await _handler.Handle(cmd, CancellationToken.None);

        integracion.ApiKey.Should().Be("existing-key");
        _repoMock.Verify(r => r.UpdateAsync(integracion), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Missing()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ConfiguracionIntegracion?)null);

        var cmd = new ActualizarIntegracionCommand(id, "https://url.com", "key", true);
        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Should_Persist_Settings_When_Provided()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://old.url", "key", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfiguracionIntegracion>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarIntegracionCommand(id, "https://new.url", null, true,
            new Dictionary<string, string>
            {
                ["SystemUserEmail"] = "sistema@docflow.cl",
                ["PollingIntervalMinutes"] = "20",
            });
        var result = await _handler.Handle(cmd, CancellationToken.None);

        integracion.GetSetting("SystemUserEmail").Should().Be("sistema@docflow.cl");
        integracion.GetSetting("PollingIntervalMinutes").Should().Be("20");
        result.Settings.Should().ContainKey("SystemUserEmail");
    }

    [Fact]
    public async Task Should_Not_Touch_Settings_When_Null()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://old.url", "key", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfiguracionIntegracion>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarIntegracionCommand(id, "https://new.url", null, true);
        await _handler.Handle(cmd, CancellationToken.None);

        integracion.Settings.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Invalidate_Config_Cache_After_Update()
    {
        var id = Guid.NewGuid();
        var integracion = ConfiguracionIntegracion.Crear(id, "DocDigital", TipoIntegracion.DocDigital,
            "https://old.url", "key", true);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(integracion);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfiguracionIntegracion>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var cmd = new ActualizarIntegracionCommand(id, "https://new.url", null, true);
        await _handler.Handle(cmd, CancellationToken.None);

        _integracionConfigMock.Verify(c => c.Invalidate("DocDigital"), Times.Once);
    }
}
