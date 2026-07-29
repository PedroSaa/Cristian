using System.Reflection;
using DocFlow.Api.Controllers.Catalogos;
using DocFlow.Application.Admin.CatalogosLegado.Commands.ReemplazarPlantillaFlujo;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.GetPlantillaFlujo;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers.Catalogos;

public class AdminCatalogosPlantillasFlujoControllerTests
{
    private readonly Mock<ISender> _mediator = new();
    private readonly AdminCatalogosPlantillasController _controller;

    public AdminCatalogosPlantillasFlujoControllerTests()
    {
        _controller = new AdminCatalogosPlantillasController(
            _mediator.Object,
            Mock.Of<IOnlyOfficeJwtService>(),
            Mock.Of<IOnlyOfficeDocumentService>(),
            Mock.Of<IIntegracionConfigService>(),
            Mock.Of<IHttpClientFactory>());
    }

    private static PlantillaFlujoPasoDto SampleDto(int orden = 1) =>
        new(Guid.NewGuid(), orden, "Autorizar", "Departamento", Guid.NewGuid(), "Finanzas", true);

    [Fact]
    public async Task GetFlujo_ShouldReturnOkWithData()
    {
        var data = new List<PlantillaFlujoPasoDto> { SampleDto() };
        _mediator.Setup(m => m.Send(It.IsAny<GetPlantillaFlujoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var result = await _controller.GetFlujo("FORM-1", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task UpdateFlujo_ShouldReturnOkWithResultingDtos()
    {
        var data = new List<PlantillaFlujoPasoDto> { SampleDto(1), SampleDto(2) };
        _mediator.Setup(m => m.Send(It.IsAny<ReemplazarPlantillaFlujoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var req = new ReemplazarPlantillaFlujoRequest(
            [new PlantillaFlujoPasoInput(1, "Autorizar", "Departamento", Guid.NewGuid(), true)]);

        var result = await _controller.UpdateFlujo("FORM-1", req, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task UpdateFlujo_ShouldReturnNotFound_WhenTemplateMissing()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ReemplazarPlantillaFlujoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Plantilla no encontrada."));

        var result = await _controller.UpdateFlujo("FORM-1",
            new ReemplazarPlantillaFlujoRequest([]), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateFlujo_ShouldReturnBadRequest_OnValidationException()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ReemplazarPlantillaFlujoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("Orden duplicado."));

        var result = await _controller.UpdateFlujo("FORM-1",
            new ReemplazarPlantillaFlujoRequest([]), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetFlujo_ShouldRequireVerPermission()
    {
        var attr = typeof(AdminCatalogosPlantillasController)
            .GetMethod(nameof(AdminCatalogosPlantillasController.GetFlujo))!
            .GetCustomAttribute<HasPermissionAttribute>();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"{HasPermissionAttribute.PolicyPrefix}admin.catalogos.ver");
    }

    [Fact]
    public void UpdateFlujo_ShouldRequireEditarPermission()
    {
        var attr = typeof(AdminCatalogosPlantillasController)
            .GetMethod(nameof(AdminCatalogosPlantillasController.UpdateFlujo))!
            .GetCustomAttribute<HasPermissionAttribute>();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"{HasPermissionAttribute.PolicyPrefix}admin.catalogos.editar");
    }
}
