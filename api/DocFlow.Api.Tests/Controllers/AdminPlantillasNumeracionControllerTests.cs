using DocFlow.Api.Controllers;
using DocFlow.Application.Numeracion.Commands.CreatePlantilla;
using DocFlow.Application.Numeracion.Commands.DeletePlantilla;
using DocFlow.Application.Numeracion.Commands.SetPlantillaActiva;
using DocFlow.Application.Numeracion.Commands.TogglePlantilla;
using DocFlow.Application.Numeracion.Commands.UpdatePlantilla;
using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Application.Numeracion.Queries.ListPlantillas;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminPlantillasNumeracionControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminPlantillasNumeracionController _controller;

    public AdminPlantillasNumeracionControllerTests()
    {
        _controller = new AdminPlantillasNumeracionController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_Should_Return_200_WithPlantillas()
    {
        var plantillas = new List<PlantillaNumeracionDto> { new(1, "Solo correlativo", "{correlativo}", true, false, false, false, "CONTINUO", "AL_INGRESAR", 0, 0) };
        _mediatorMock.Setup(x => x.Send(It.IsAny<ListPlantillasQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantillas);

        var result = await _controller.List(soloActivos: true, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<List<PlantillaNumeracionDto>>().Subject.Should().HaveCount(1);
        _mediatorMock.Verify(x => x.Send(It.IsAny<ListPlantillasQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Should_Return_201_WhenValid()
    {
        var dto = new PlantillaNumeracionDto(7, "Nueva", "{correlativo}/{ano}", true, false, false, false, "CONTINUO", "AL_INGRESAR", 0, 0);
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreatePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(new CreatePlantillaRequest(7, "Nueva", "{correlativo}/{ano}"), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Value.Should().Be(dto);
        _mediatorMock.Verify(x => x.Send(It.Is<CreatePlantillaCommand>(c => c.Id == 7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Should_Return_400_OnValidationException()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreatePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("inválido"));

        var result = await _controller.Create(new CreatePlantillaRequest(0, "", ""), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_409_OnInvalidOperation()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreatePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ya existe"));

        var result = await _controller.Create(new CreatePlantillaRequest(1, "x", "y"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_200_WhenValid()
    {
        var dto = new PlantillaNumeracionDto(3, "Editada", "{correlativo}", true, false, false, false, "CONTINUO", "AL_INGRESAR", 0, 0);
        _mediatorMock.Setup(x => x.Send(It.IsAny<UpdatePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(3, new UpdatePlantillaRequest("Editada", "{correlativo}"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
        _mediatorMock.Verify(x => x.Send(It.Is<UpdatePlantillaCommand>(c => c.Id == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_Should_Return_404_WhenNotFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<UpdatePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("no existe"));

        var result = await _controller.Update(99, new UpdatePlantillaRequest("x", "y"), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Toggle_Should_Return_200_WhenFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<TogglePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Toggle(5, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<TogglePlantillaCommand>(c => c.Id == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Toggle_Should_Return_404_WhenNotFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<TogglePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("no existe"));

        var result = await _controller.Toggle(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Activar_Should_Return_200_WhenFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<SetPlantillaActivaCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Activar(3, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<SetPlantillaActivaCommand>(c => c.Id == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activar_Should_Return_404_WhenNotFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<SetPlantillaActivaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("no existe"));

        var result = await _controller.Activar(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_204_WhenDeleted()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<DeletePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(5, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(x => x.Send(It.Is<DeletePlantillaCommand>(c => c.Id == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Should_Return_404_WhenNotFound()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<DeletePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("no existe"));

        var result = await _controller.Delete(99, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_409_WhenActiva()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<DeletePlantillaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No se puede eliminar la plantilla activa del sistema."));

        var result = await _controller.Delete(3, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public void Tokens_Should_Return_200_WithCatalogo()
    {
        var result = _controller.Tokens();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<TokenNumeracion>>()
            .Subject.Should().NotBeEmpty();
    }
}
