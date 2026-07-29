using DocFlow.Application.Admin.CatalogosLegado.Commands.ReemplazarPlantillaFlujo;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Admin.CatalogosLegado;

public class ReemplazarPlantillaFlujoCommandHandlerTests
{
    private readonly Mock<IPlantillaFlujoRepository> _flujo = new();
    private readonly Mock<ISeForplaRepository> _plantillas = new();
    private readonly Mock<IResponsableFlujoNombreResolver> _nombres = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly ReemplazarPlantillaFlujoCommandHandler _handler;

    public ReemplazarPlantillaFlujoCommandHandlerTests()
    {
        _currentUser.SetupGet(u => u.UserId).Returns(Guid.NewGuid());
        _nombres.Setup(n => n.ResolverNombresAsync(It.IsAny<ResponsableFlujoTipo>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _handler = new ReemplazarPlantillaFlujoCommandHandler(
            _flujo.Object, _plantillas.Object, _nombres.Object,
            _auditoria.Object, _currentUser.Object,
            Mock.Of<ILogger<ReemplazarPlantillaFlujoCommandHandler>>());
    }

    private static ReemplazarPlantillaFlujoCommand SampleCommand(string codForm = "F") => new(
        codForm,
        [
            new PlantillaFlujoPasoInput(1, "Autorizar", "Departamento", Guid.NewGuid(), true),
            new PlantillaFlujoPasoInput(2, "Firmar", "Usuario", Guid.NewGuid(), true),
        ]);

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFound_WhenTemplateDoesNotExist()
    {
        _plantillas.Setup(p => p.ExistsAsync("F")).ReturnsAsync(false);

        var act = () => _handler.Handle(SampleCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _flujo.Verify(r => r.ReemplazarAsync(It.IsAny<string>(), It.IsAny<IEnumerable<PlantillaFlujoPaso>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReplaceWorkflow_AndAudit_AndReturnDtos()
    {
        _plantillas.Setup(p => p.ExistsAsync("F")).ReturnsAsync(true);
        IEnumerable<PlantillaFlujoPaso>? capturados = null;
        _flujo.Setup(r => r.ReemplazarAsync("F", It.IsAny<IEnumerable<PlantillaFlujoPaso>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<PlantillaFlujoPaso>, CancellationToken>((_, pasos, _) => capturados = pasos.ToList())
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(SampleCommand(), CancellationToken.None);

        capturados.Should().NotBeNull();
        capturados!.Should().HaveCount(2);
        capturados!.Select(p => p.CodForm).Should().AllBe("F");

        result.Should().HaveCount(2);
        result.Select(r => r.Orden).Should().ContainInOrder(1, 2);
        result[0].TipoAccion.Should().Be("Autorizar");
        result[0].ResponsableTipo.Should().Be("Departamento");

        _auditoria.Verify(a => a.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.Accion == "PlantillaFlujoActualizado" && r.Entidad == "PlantillaFlujoPaso" && r.EntidadId == "F")),
            Times.Once);
    }

    [Fact]
    public void Validator_ShouldReject_InvalidEnums()
    {
        var validator = new ReemplazarPlantillaFlujoCommandValidator();

        var result = validator.Validate(new ReemplazarPlantillaFlujoCommand("F",
            [new PlantillaFlujoPasoInput(1, "Bailar", "Extraterrestre", Guid.NewGuid(), true)]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("TipoAccion"));
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ResponsableTipo"));
    }

    [Fact]
    public void Validator_ShouldReject_DuplicateOrden()
    {
        var validator = new ReemplazarPlantillaFlujoCommandValidator();

        var result = validator.Validate(new ReemplazarPlantillaFlujoCommand("F",
        [
            new PlantillaFlujoPasoInput(1, "Firmar", "Usuario", Guid.NewGuid(), true),
            new PlantillaFlujoPasoInput(1, "Visar", "Rol", Guid.NewGuid(), true),
        ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("no puede repetirse"));
    }

    [Fact]
    public void Validator_ShouldReject_OrdenLessThanOne_AndEmptyResponsableId()
    {
        var validator = new ReemplazarPlantillaFlujoCommandValidator();

        var result = validator.Validate(new ReemplazarPlantillaFlujoCommand("F",
            [new PlantillaFlujoPasoInput(0, "Firmar", "Usuario", Guid.Empty, true)]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Orden"));
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ResponsableId"));
    }
}
