using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands.CreatePlantilla;
using DocFlow.Application.Numeracion.Commands.TogglePlantilla;
using DocFlow.Application.Numeracion.Commands.UpdatePlantilla;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocFlow.Application.Tests.Numeracion.Commands;

public class PlantillaCommandAuditTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Mock<IPlantillaService> _plantillaService = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    public PlantillaCommandAuditTests()
    {
        _currentUser.Setup(x => x.UserId).Returns(_actorId);
        _auditoria.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreatePlantilla_Writes_Audit_With_Actor_And_Created_Values()
    {
        var created = new PlantillaNumeracion(12, "Resolucion", "RES-{YYYY}-{SEQ}");
        _plantillaService.Setup(x => x.CrearAsync(12, "Resolucion", "RES-{YYYY}-{SEQ}",
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var handler = new CreatePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new CreatePlantillaCommand(12, "Resolucion", "RES-{YYYY}-{SEQ}"), CancellationToken.None);

        VerifyAudit("CrearPlantillaNumeracion", "12", "descripcion=Resolucion", "patron=RES-{YYYY}-{SEQ}", "activo=True");
    }

    [Fact]
    public async Task CreatePlantilla_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new CreatePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new CreatePlantillaCommand(12, "Resolucion", "RES"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _plantillaService.Verify(x => x.CrearAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditoria.Verify(x => x.AddAsync(It.IsAny<RegistroAuditoria>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePlantilla_Writes_Audit_With_Before_And_After_Values()
    {
        var before = new PlantillaNumeracion(12, "Resolucion", "RES-{SEQ}");
        var after = new PlantillaNumeracion(12, "Resolucion Interna", "RI-{SEQ}");
        _plantillaService.Setup(x => x.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(before);
        _plantillaService.Setup(x => x.ActualizarAsync(12, "Resolucion Interna", "RI-{SEQ}",
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(after);

        var handler = new UpdatePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new UpdatePlantillaCommand(12, "Resolucion Interna", "RI-{SEQ}"), CancellationToken.None);

        VerifyAudit("ActualizarPlantillaNumeracion", "12", "descripcionAntes=Resolucion", "descripcionDespues=Resolucion Interna", "patronAntes=RES-{SEQ}", "patronDespues=RI-{SEQ}");
    }

    [Fact]
    public async Task UpdatePlantilla_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new UpdatePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new UpdatePlantillaCommand(12, "Resolucion", "RES"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _plantillaService.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _plantillaService.Verify(x => x.ActualizarAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TogglePlantilla_Writes_Audit_With_Previous_And_New_Active_State()
    {
        var before = new PlantillaNumeracion(12, "Resolucion", "RES");
        _plantillaService.Setup(x => x.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(before);
        _plantillaService.Setup(x => x.ToggleActivoAsync(12, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new TogglePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);
        await handler.Handle(new TogglePlantillaCommand(12), CancellationToken.None);

        VerifyAudit("AlternarPlantillaNumeracion", "12", "activoAntes=True", "activoDespues=False");
    }

    [Fact]
    public async Task TogglePlantilla_Missing_Actor_Fails_Before_Mutation()
    {
        _currentUser.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new TogglePlantillaHandler(_plantillaService.Object, _auditoria.Object, _currentUser.Object);

        var act = async () => await handler.Handle(new TogglePlantillaCommand(12), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _plantillaService.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _plantillaService.Verify(x => x.ToggleActivoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyAudit(string accion, string entidadId, params string[] detailParts)
    {
        _auditoria.Verify(x => x.AddAsync(It.Is<RegistroAuditoria>(r =>
            r.UsuarioId == _actorId &&
            r.Accion == accion &&
            r.Entidad == "PlantillaNumeracion" &&
            r.EntidadId == entidadId &&
            detailParts.All(part => r.Detalle.Contains(part)))), Times.Once);
    }
}
