using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Admin.Configuracion.Queries.GetConfiguracion;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Configuracion.Queries.GetConfiguracion;

public class GetConfiguracionQueryHandlerTests
{
    private readonly Mock<IConfiguracionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly GetConfiguracionQueryHandler _handler;

    public GetConfiguracionQueryHandlerTests()
    {
        _handler = new GetConfiguracionQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Should_Return_ConfiguracionDto_When_Clave_Exists()
    {
        var config = ConfiguracionSistema.Crear(Guid.NewGuid(), "MAX_INTENTOS", "5", "Intentos máximos");
        _repoMock.Setup(r => r.GetByClaveAsync("MAX_INTENTOS")).ReturnsAsync(config);

        var result = await _handler.Handle(new GetConfiguracionQuery("MAX_INTENTOS"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Clave.Should().Be("MAX_INTENTOS");
        result.Valor.Should().Be("5");
        result.Descripcion.Should().Be("Intentos máximos");
    }

    [Fact]
    public async Task Should_Return_LoginBranding_Data_When_Keys_Exist()
    {
        var background = ConfiguracionSistema.Crear(Guid.NewGuid(), "LoginBackgroundUrl", "/branding/login-background.png", "URL del fondo de login");
        _repoMock.Setup(r => r.GetByClaveAsync("LoginBackgroundUrl")).ReturnsAsync(background);

        var result = await _handler.Handle(new GetConfiguracionQuery("LoginBackgroundUrl"), CancellationToken.None);

        result.Valor.Should().Be("/branding/login-background.png");
        result.Descripcion.Should().Be("URL del fondo de login");
    }

    [Fact]
    public async Task Should_Throw_KeyNotFoundException_When_Clave_Missing()
    {
        _repoMock.Setup(r => r.GetByClaveAsync("NO_EXISTE")).ReturnsAsync((ConfiguracionSistema?)null);

        var act = async () => await _handler.Handle(new GetConfiguracionQuery("NO_EXISTE"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Configuración con clave 'NO_EXISTE' no encontrada.");
    }
}
