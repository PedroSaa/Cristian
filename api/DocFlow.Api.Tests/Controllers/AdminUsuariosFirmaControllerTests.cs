using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminUsuariosFirmaControllerTests
{
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly AdminUsuariosFirmaController _controller;
    private static readonly Guid UsuarioId = Guid.NewGuid();

    public AdminUsuariosFirmaControllerTests()
    {
        _controller = new AdminUsuariosFirmaController(_mediatorMock.Object);
    }

    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2];

    [Fact]
    public async Task GetMetadata_Should_Return_200_With_Dto()
    {
        var dto = new FirmaUsuarioMetadataDto(UsuarioId, true, true, "JJP", "image/png", 100, DateTime.UtcNow, DateTime.UtcNow);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaUsuarioQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetMetadata(UsuarioId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<FirmaUsuarioMetadataDto>().Which.TieneFirma.Should().BeTrue();
    }

    [Fact]
    public async Task GetImagen_Should_Return_File_When_Exists()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaImagenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FirmaImagenDto(PngBytes(), "image/png"));

        var result = await _controller.GetImagen(UsuarioId, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("image/png");
        file.FileContents.Should().BeEquivalentTo(PngBytes());
    }

    [Fact]
    public async Task GetImagen_Should_Return_404_When_None()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaImagenQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("El usuario no tiene una firma configurada."));

        var result = await _controller.GetImagen(UsuarioId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Guardar_Should_Return_200_And_Forward_Command()
    {
        var dto = new FirmaUsuarioMetadataDto(UsuarioId, true, false, "AB", "image/png", 10, DateTime.UtcNow, DateTime.UtcNow);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var req = new GuardarFirmaRequest(Convert.ToBase64String(PngBytes()), "image/png", "1234", "AB");

        var result = await _controller.Guardar(UsuarioId, req, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<GuardarFirmaUsuarioCommand>(c =>
                c.UsuarioId == UsuarioId && c.ContentType == "image/png" && c.Clave == "1234" && c.Sigla == "AB"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Guardar_Should_Forward_Null_Image_When_Base64_Omitted()
    {
        var dto = new FirmaUsuarioMetadataDto(UsuarioId, true, false, "AB", "image/png", 10, DateTime.UtcNow, DateTime.UtcNow);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        // No image (partial update): only sigla is being changed.
        var req = new GuardarFirmaRequest(null, null, null, "AB");

        var result = await _controller.Guardar(UsuarioId, req, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<GuardarFirmaUsuarioCommand>(c =>
                c.UsuarioId == UsuarioId && c.Imagen == null && c.ContentType == null && c.Sigla == "AB"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Guardar_Should_Return_400_When_Base64_Invalid()
    {
        var req = new GuardarFirmaRequest("no-es-base64!!!", "image/png");

        var result = await _controller.Guardar(UsuarioId, req, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Guardar_Should_Return_400_When_Validation_Fails()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("La firma debe ser una imagen PNG o JPEG."));
        var req = new GuardarFirmaRequest(Convert.ToBase64String(PngBytes()), "application/pdf");

        var result = await _controller.Guardar(UsuarioId, req, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Eliminar_Should_Return_204_When_Ok()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<EliminarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Eliminar(UsuarioId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Eliminar_Should_Return_404_When_None()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<EliminarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("El usuario no tiene una firma configurada."));

        var result = await _controller.Eliminar(UsuarioId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void Controller_Should_Require_Authorize_And_Mfa()
    {
        var type = typeof(AdminUsuariosFirmaController);
        type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty();
        type.GetCustomAttributes(typeof(RequireMfaAttribute), true).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(nameof(AdminUsuariosFirmaController.GetMetadata), "admin.usuarios.ver")]
    [InlineData(nameof(AdminUsuariosFirmaController.GetImagen), "admin.usuarios.ver")]
    [InlineData(nameof(AdminUsuariosFirmaController.Guardar), "admin.usuarios.editar")]
    [InlineData(nameof(AdminUsuariosFirmaController.Eliminar), "admin.usuarios.editar")]
    public void Endpoints_Should_Use_Expected_Permissions(string methodName, string expectedPermission)
    {
        var method = typeof(AdminUsuariosFirmaController).GetMethod(methodName)!;
        var attr = method.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>()
            .Single();

        attr.Policy.Should().Be($"Permission:{expectedPermission}");
    }
}
