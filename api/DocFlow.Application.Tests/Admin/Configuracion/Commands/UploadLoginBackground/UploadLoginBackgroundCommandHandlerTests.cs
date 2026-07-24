using DocFlow.Application.Admin.Configuracion.Commands.UploadLoginBackground;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Branding;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UploadLoginBackground;

public class UploadLoginBackgroundCommandHandlerTests
{
    private readonly Mock<IBrandingLogoStorageService> _storageMock = new(MockBehavior.Strict);
    private readonly Mock<IConfiguracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUser> _currentUserMock = new(MockBehavior.Strict);
    private readonly UploadLoginBackgroundCommandHandler _handler;

    public UploadLoginBackgroundCommandHandlerTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _handler = new UploadLoginBackgroundCommandHandler(_storageMock.Object, _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Should_Save_LoginBackground_And_Upsert_Config()
    {
        var cmd = new UploadLoginBackgroundCommand(new byte[] { 0x01, 0x02 }, "login-background.png", "image/png");

        _storageMock.Setup(s => s.SaveLoginBackgroundAsync(cmd.Content, cmd.FileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/branding/login-background.png");
        _repoMock.Setup(r => r.GetByClaveAsync(BrandingConfigKeys.LoginBackgroundMode)).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.GetByClaveAsync("LoginBackgroundUrl")).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Clave.Should().Be("LoginBackgroundUrl");
        result.Valor.Should().Be("/branding/login-background.png");
        result.Descripcion.Should().Be("URL del fondo de login");
        _storageMock.Verify(s => s.SaveLoginBackgroundAsync(cmd.Content, cmd.FileName, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.UpsertAsync(It.Is<ConfiguracionSistema>(c => c.Clave == BrandingConfigKeys.LoginBackgroundMode && c.Valor == LoginBackgroundCatalog.ModeImage)), Times.Once);
        _repoMock.Verify(r => r.UpsertAsync(It.Is<ConfiguracionSistema>(c => c.Clave == "LoginBackgroundUrl" && c.Valor == "/branding/login-background.png")), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r => r.Accion == "UploadLoginBackground")), Times.Once);
    }
}
