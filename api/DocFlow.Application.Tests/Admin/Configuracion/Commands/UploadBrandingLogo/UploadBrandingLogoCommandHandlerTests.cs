using DocFlow.Application.Admin.Configuracion.Commands.UploadBrandingLogo;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Commands.UploadBrandingLogo;

public class UploadBrandingLogoCommandHandlerTests
{
    private readonly Mock<IBrandingLogoStorageService> _storageMock = new();
    private readonly Mock<IConfiguracionRepository> _repoMock = new();
    private readonly Mock<IAuditoriaRepository> _auditoriaMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _adminId = Guid.NewGuid();

    private UploadBrandingLogoCommandHandler CreateSut()
    {
        _currentUserMock.SetupGet(c => c.UserId).Returns(_adminId);
        return new(_storageMock.Object, _repoMock.Object, _auditoriaMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_CreaEntrada_CuandoNoExiste()
    {
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<byte[]>(), "logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/branding/logo.png");
        _repoMock.Setup(r => r.GetByClaveAsync(It.IsAny<string>())).ReturnsAsync((ConfiguracionSistema?)null);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(
            new UploadBrandingLogoCommand(new byte[] { 1, 2, 3 }, "logo.png", "image/png"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Valor.Should().Be("/branding/logo.png");
        _repoMock.Verify(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>()), Times.Once);
        _auditoriaMock.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(x => x.UsuarioId == _adminId)), Times.Once);
    }

    [Fact]
    public async Task Handle_ActualizaEntrada_CuandoYaExiste()
    {
        var existing = ConfiguracionSistema.Crear(Guid.NewGuid(), "LogoUrl", "/old.png", "URL del logo institucional");
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/branding/new.png");
        _repoMock.Setup(r => r.GetByClaveAsync(It.IsAny<string>())).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpsertAsync(It.IsAny<ConfiguracionSistema>())).Returns(Task.CompletedTask);
        _auditoriaMock.Setup(a => a.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(
            new UploadBrandingLogoCommand(new byte[] { 1 }, "logo.png", "image/png"),
            CancellationToken.None);

        result.Valor.Should().Be("/branding/new.png");
        result.Id.Should().Be(existing.Id);
        _repoMock.Verify(r => r.UpsertAsync(existing), Times.Once);
    }
}
