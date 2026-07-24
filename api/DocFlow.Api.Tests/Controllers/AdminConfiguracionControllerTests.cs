using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Configuracion.Commands.UpsertConfiguracion;
using DocFlow.Application.Admin.Configuracion.Commands.UploadBrandingLogo;
using DocFlow.Application.Admin.Configuracion.Commands.UploadLoginBackground;
using DocFlow.Application.Admin.Configuracion.DTOs;
using DocFlow.Application.Admin.Configuracion.Queries.GetConfiguracion;
using DocFlow.Application.Admin.Configuracion.Queries.ListConfiguracion;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Branding;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminConfiguracionControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminConfiguracionController _controller;

    public AdminConfiguracionControllerTests()
    {
        _controller = new AdminConfiguracionController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_Should_Return_200_WithListOfConfiguracion()
    {
        var configs = new List<ConfiguracionDto>
        {
            new(Guid.NewGuid(), "MAX_INTENTOS", "5", "Intentos máximos", DateTime.UtcNow),
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListConfiguracionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ConfiguracionDto>)configs);

        var result = await _controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<ConfiguracionDto>>().Subject;
        value.Should().HaveCount(1);
        value[0].Clave.Should().Be("MAX_INTENTOS");
    }

    [Fact]
    public async Task GetByClave_Should_Return_200_WhenClaveExists()
    {
        var configDto = new ConfiguracionDto(Guid.NewGuid(), "MAX_INTENTOS", "5", "Intentos máximos", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetConfiguracionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        var result = await _controller.GetByClave("MAX_INTENTOS", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<ConfiguracionDto>().Subject;
        value.Clave.Should().Be("MAX_INTENTOS");
    }

    [Fact]
    public async Task GetByClave_Should_Return_404_WhenKeyNotFoundException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetConfiguracionQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Configuración con clave 'NO_EXISTE' no encontrada."));

        var result = await _controller.GetByClave("NO_EXISTE", CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = "Configuración con clave 'NO_EXISTE' no encontrada." });
    }

    [Fact]
    public async Task Upsert_Should_Return_200_WhenMediatorCompletes()
    {
        var req = new UpsertConfiguracionRequest("MAX_INTENTOS", "5", "Intentos máximos");
        var configDto = new ConfiguracionDto(Guid.NewGuid(), "MAX_INTENTOS", "5", "Intentos máximos", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpsertConfiguracionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        var result = await _controller.Upsert(req, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<ConfiguracionDto>().Subject;
        value.Clave.Should().Be("MAX_INTENTOS");
    }

    [Fact]
    public async Task Upsert_Should_Return_400_WhenValidationException()
    {
        var req = new UpsertConfiguracionRequest("", "", null);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpsertConfiguracionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("La clave es obligatoria."));

        var result = await _controller.Upsert(req, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { mensaje = "La clave es obligatoria." });
    }

    [Fact]
    public async Task UploadLogo_Should_Return_200_WhenMediatorCompletes()
    {
        var dto = new ConfiguracionDto(Guid.NewGuid(), "LogoUrl", "/branding/logo.png", "URL del logo institucional", DateTime.UtcNow);
        var file = CreateFormFile("logo.png", "image/png", new byte[] { 0x01, 0x02, 0x03 });

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UploadBrandingLogoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.UploadLogo(file, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<ConfiguracionDto>().Subject;
        value.Clave.Should().Be("LogoUrl");
        value.Valor.Should().Be("/branding/logo.png");
    }

    [Fact]
    public async Task UploadLogo_Should_Return_400_WhenFileIsEmpty()
    {
        var file = CreateFormFile("logo.png", "image/png", Array.Empty<byte>());

        var result = await _controller.UploadLogo(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadLogo_Should_Return_400_WhenFileIsTooLarge()
    {
        var file = CreateFormFile("logo.png", "image/png", new byte[BrandingImageUploadValidation.MaxImageBytes + 1]);

        var result = await _controller.UploadLogo(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<UploadBrandingLogoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLoginBackground_Should_Return_200_WhenMediatorCompletes()
    {
        var dto = new ConfiguracionDto(Guid.NewGuid(), "LoginBackgroundUrl", "/branding/login-background.png", "URL del fondo de login", DateTime.UtcNow);
        var file = CreateFormFile("login-background.png", "image/png", new byte[] { 0x01, 0x02, 0x03 }, "loginBackground");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UploadLoginBackgroundCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.UploadLoginBackground(file, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<ConfiguracionDto>().Subject;
        value.Clave.Should().Be("LoginBackgroundUrl");
        value.Valor.Should().Be("/branding/login-background.png");
    }

    [Fact]
    public async Task UploadLoginBackground_Should_Return_400_WhenFileIsEmpty()
    {
        var file = CreateFormFile("login-background.png", "image/png", Array.Empty<byte>(), "loginBackground");

        var result = await _controller.UploadLoginBackground(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadLoginBackground_Should_Return_400_WhenFileIsTooLarge()
    {
        var file = CreateFormFile("login-background.png", "image/png", new byte[BrandingImageUploadValidation.MaxImageBytes + 1], "loginBackground");

        var result = await _controller.UploadLoginBackground(file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<UploadLoginBackgroundCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(AdminConfiguracionController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public void Controller_Should_Have_RequireMfaAttribute()
    {
        var attr = typeof(AdminConfiguracionController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData("List", "admin.config.ver")]
    [InlineData("GetByClave", "admin.config.ver")]
    [InlineData("Upsert", "admin.config.editar")]
    [InlineData("UploadLogo", "admin.config.editar")]
    [InlineData("UploadLoginBackground", "admin.config.editar")]
    public void Action_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        var method = typeof(AdminConfiguracionController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull($"Action {actionName} should have [HasPermission(\"{expectedPermission}\")]");
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes, string name = "logo")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, name, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
