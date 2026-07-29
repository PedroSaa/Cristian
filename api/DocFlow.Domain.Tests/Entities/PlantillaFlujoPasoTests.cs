using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocFlow.Domain.Tests.Entities;

public class PlantillaFlujoPasoTests
{
    [Fact]
    public void Crear_ShouldBuildStep_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var responsableId = Guid.NewGuid();

        var paso = PlantillaFlujoPaso.Crear(
            id, " FORM-1 ", 1, TipoAccionFlujo.Autorizar, ResponsableFlujoTipo.Departamento, responsableId);

        paso.Id.Should().Be(id);
        paso.CodForm.Should().Be("FORM-1");
        paso.Orden.Should().Be(1);
        paso.TipoAccion.Should().Be(TipoAccionFlujo.Autorizar);
        paso.ResponsableTipo.Should().Be(ResponsableFlujoTipo.Departamento);
        paso.ResponsableId.Should().Be(responsableId);
        paso.Obligatorio.Should().BeTrue();
    }

    [Fact]
    public void Crear_ShouldDefaultObligatorio_ToTrue()
    {
        var paso = PlantillaFlujoPaso.Crear(
            Guid.NewGuid(), "F", 2, TipoAccionFlujo.Firmar, ResponsableFlujoTipo.Usuario, Guid.NewGuid());

        paso.Obligatorio.Should().BeTrue();
    }

    [Fact]
    public void Crear_ShouldRespectObligatorio_WhenFalse()
    {
        var paso = PlantillaFlujoPaso.Crear(
            Guid.NewGuid(), "F", 2, TipoAccionFlujo.Revisar, ResponsableFlujoTipo.Rol, Guid.NewGuid(), obligatorio: false);

        paso.Obligatorio.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Crear_ShouldThrow_WhenCodFormIsEmpty(string? codForm)
    {
        var act = () => PlantillaFlujoPaso.Crear(
            Guid.NewGuid(), codForm!, 1, TipoAccionFlujo.Firmar, ResponsableFlujoTipo.Usuario, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crear_ShouldThrow_WhenOrdenIsLessThanOne(int orden)
    {
        var act = () => PlantillaFlujoPaso.Crear(
            Guid.NewGuid(), "F", orden, TipoAccionFlujo.Firmar, ResponsableFlujoTipo.Usuario, Guid.NewGuid());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Crear_ShouldThrow_WhenResponsableIdIsEmpty()
    {
        var act = () => PlantillaFlujoPaso.Crear(
            Guid.NewGuid(), "F", 1, TipoAccionFlujo.Firmar, ResponsableFlujoTipo.Usuario, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
