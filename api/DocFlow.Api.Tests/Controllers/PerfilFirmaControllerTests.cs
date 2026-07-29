using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.Commands.GuardarFirmaUsuario;
using DocFlow.Application.Admin.Usuarios.Firma.DTOs;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaImagen;
using DocFlow.Application.Admin.Usuarios.Firma.Queries.GetFirmaUsuario;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class PerfilFirmaControllerTests
{
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly PerfilFirmaController _controller;
    private static readonly Guid CurrentUserId = Guid.NewGuid();

    public PerfilFirmaControllerTests()
    {
        _currentUserMock.SetupGet(x => x.UserId).Returns(CurrentUserId);
        _controller = new PerfilFirmaController(_mediatorMock.Object, _currentUserMock.Object);
    }

    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2];

    [Fact]
    public async Task GetMetadata_Should_Query_For_Current_User()
    {
        var dto = new FirmaUsuarioMetadataDto(CurrentUserId, true, true, "YO", "image/png", 100, DateTime.UtcNow, DateTime.UtcNow);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaUsuarioQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetMetadata(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<GetFirmaUsuarioQuery>(q => q.UsuarioId == CurrentUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetImagen_Should_Return_File_For_Current_User()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaImagenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FirmaImagenDto(PngBytes(), "image/png"));

        var result = await _controller.GetImagen(CancellationToken.None);

        result.Should().BeOfType<FileContentResult>().Which.ContentType.Should().Be("image/png");
        _mediatorMock.Verify(x => x.Send(
            It.Is<GetFirmaImagenQuery>(q => q.UsuarioId == CurrentUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetImagen_Should_Return_404_When_None()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetFirmaImagenQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("El usuario no tiene una firma configurada."));

        var result = await _controller.GetImagen(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Guardar_Should_Forward_Command_With_Current_User()
    {
        var dto = new FirmaUsuarioMetadataDto(CurrentUserId, true, true, "YO", "image/png", 10, DateTime.UtcNow, DateTime.UtcNow);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var req = new GuardarFirmaRequest(Convert.ToBase64String(PngBytes()), "image/png", "mi-pin", "YO");

        var result = await _controller.Guardar(req, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<GuardarFirmaUsuarioCommand>(c =>
                c.UsuarioId == CurrentUserId && c.Clave == "mi-pin" && c.Sigla == "YO"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Guardar_Should_Return_400_When_Base64_Invalid()
    {
        var req = new GuardarFirmaRequest("no-es-base64!!!", "image/png");

        var result = await _controller.Guardar(req, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<GuardarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Eliminar_Should_Return_204_For_Current_User()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<EliminarFirmaUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Eliminar(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<EliminarFirmaUsuarioCommand>(c => c.UsuarioId == CurrentUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Controller_Should_Require_Authorize_Without_Admin_Permission()
    {
        var type = typeof(PerfilFirmaController);
        // Self-service: authenticated only, NO admin permission, NO MFA gate.
        type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty();
        type.GetCustomAttributes(typeof(RequireMfaAttribute), true).Should().BeEmpty();
        foreach (var method in new[] { nameof(PerfilFirmaController.GetMetadata), nameof(PerfilFirmaController.Guardar), nameof(PerfilFirmaController.Eliminar) })
        {
            typeof(PerfilFirmaController).GetMethod(method)!
                .GetCustomAttributes(typeof(HasPermissionAttribute), true).Should().BeEmpty();
        }
    }
}
