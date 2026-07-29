using DocFlow.Application.Admin.CatalogosLegado.Queries.GetPlantillaFlujo;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado;

public class GetPlantillaFlujoQueryHandlerTests
{
    private readonly Mock<IPlantillaFlujoRepository> _flujo = new();
    private readonly Mock<IResponsableFlujoNombreResolver> _nombres = new();
    private readonly GetPlantillaFlujoQueryHandler _handler;

    public GetPlantillaFlujoQueryHandlerTests()
    {
        _handler = new GetPlantillaFlujoQueryHandler(_flujo.Object, _nombres.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoSteps()
    {
        _flujo.Setup(r => r.GetByCodFormAsync("F", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetPlantillaFlujoQuery("F"), CancellationToken.None);

        result.Should().BeEmpty();
        _nombres.Verify(n => n.ResolverNombresAsync(
            It.IsAny<ResponsableFlujoTipo>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldResolveNames_ForAllThreeResponsibleTypes()
    {
        var usuarioId = Guid.NewGuid();
        var rolId = Guid.NewGuid();
        var depId = Guid.NewGuid();

        var pasos = new List<PlantillaFlujoPaso>
        {
            PlantillaFlujoPaso.Crear(Guid.NewGuid(), "F", 1, TipoAccionFlujo.Autorizar, ResponsableFlujoTipo.Departamento, depId),
            PlantillaFlujoPaso.Crear(Guid.NewGuid(), "F", 2, TipoAccionFlujo.Firmar, ResponsableFlujoTipo.Usuario, usuarioId),
            PlantillaFlujoPaso.Crear(Guid.NewGuid(), "F", 3, TipoAccionFlujo.Visar, ResponsableFlujoTipo.Rol, rolId),
        };
        _flujo.Setup(r => r.GetByCodFormAsync("F", It.IsAny<CancellationToken>())).ReturnsAsync(pasos);

        _nombres.Setup(n => n.ResolverNombresAsync(ResponsableFlujoTipo.Usuario, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [usuarioId] = "Ana Perez" });
        _nombres.Setup(n => n.ResolverNombresAsync(ResponsableFlujoTipo.Rol, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [rolId] = "Director" });
        _nombres.Setup(n => n.ResolverNombresAsync(ResponsableFlujoTipo.Departamento, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [depId] = "Finanzas" });

        var result = await _handler.Handle(new GetPlantillaFlujoQuery("F"), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(r => r.Orden).Should().ContainInOrder(1, 2, 3);
        result[0].ResponsableNombre.Should().Be("Finanzas");
        result[0].TipoAccion.Should().Be("Autorizar");
        result[0].ResponsableTipo.Should().Be("Departamento");
        result[1].ResponsableNombre.Should().Be("Ana Perez");
        result[2].ResponsableNombre.Should().Be("Director");
    }

    [Fact]
    public async Task Handle_ShouldReturnNullName_WhenResponsibleNotFound()
    {
        var depId = Guid.NewGuid();
        _flujo.Setup(r => r.GetByCodFormAsync("F", It.IsAny<CancellationToken>()))
            .ReturnsAsync([PlantillaFlujoPaso.Crear(Guid.NewGuid(), "F", 1, TipoAccionFlujo.Revisar, ResponsableFlujoTipo.Departamento, depId)]);
        _nombres.Setup(n => n.ResolverNombresAsync(It.IsAny<ResponsableFlujoTipo>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        var result = await _handler.Handle(new GetPlantillaFlujoQuery("F"), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ResponsableNombre.Should().BeNull();
    }
}
