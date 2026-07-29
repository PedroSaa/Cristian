using DocFlow.Domain.Entities;
using DocFlow.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DocFlow.Infrastructure.Tests.Data.Configurations;

public class PlantillaFlujoPasoConfigurationTests
{
    private static DbContextOptions<DocFlowDbContext> CreateInMemoryOptions()
        => new DbContextOptionsBuilder<DocFlowDbContext>()
            .UseInMemoryDatabase($"PlantillaFlujoConfigTest_{Guid.NewGuid()}")
            .Options;

    [Fact]
    public void PlantillaFlujoPaso_ShouldMapToSnakeCaseTable()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var entityType = context.Model.FindEntityType(typeof(PlantillaFlujoPaso));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("plantilla_flujo_pasos");
        entityType.FindProperty(nameof(PlantillaFlujoPaso.CodForm))!.GetColumnName().Should().Be("cod_form");
        entityType.FindProperty(nameof(PlantillaFlujoPaso.Orden))!.GetColumnName().Should().Be("orden");
        entityType.FindProperty(nameof(PlantillaFlujoPaso.ResponsableId))!.GetColumnName().Should().Be("responsable_id");
        entityType.FindProperty(nameof(PlantillaFlujoPaso.Obligatorio))!.GetColumnName().Should().Be("obligatorio");
    }

    [Fact]
    public void PlantillaFlujoPaso_ShouldStoreEnumsAsString()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());
        var entityType = context.Model.FindEntityType(typeof(PlantillaFlujoPaso))!;

        var tipoAccion = entityType.FindProperty(nameof(PlantillaFlujoPaso.TipoAccion))!;
        tipoAccion.GetColumnName().Should().Be("tipo_accion");
        tipoAccion.GetProviderClrType().Should().Be(typeof(string));

        var responsableTipo = entityType.FindProperty(nameof(PlantillaFlujoPaso.ResponsableTipo))!;
        responsableTipo.GetColumnName().Should().Be("responsable_tipo");
        responsableTipo.GetProviderClrType().Should().Be(typeof(string));
    }

    [Fact]
    public void PlantillaFlujoPaso_ShouldHaveIndexOnCodForm()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var indexes = context.Model.FindEntityType(typeof(PlantillaFlujoPaso))!.GetIndexes();

        indexes.Should().Contain(i =>
            i.Properties.Count == 1 &&
            i.Properties[0].Name == nameof(PlantillaFlujoPaso.CodForm));
    }

    [Fact]
    public void PlantillaFlujoPaso_ShouldHaveUniqueIndexOnCodFormAndOrden()
    {
        using var context = new DocFlowDbContext(CreateInMemoryOptions());

        var index = context.Model.FindEntityType(typeof(PlantillaFlujoPaso))!
            .GetIndexes()
            .Single(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(PlantillaFlujoPaso.CodForm))
                && i.Properties.Any(p => p.Name == nameof(PlantillaFlujoPaso.Orden)));

        index.IsUnique.Should().BeTrue();
    }
}
