using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.Numeracion.Commands;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.TogglePlantilla;

public record TogglePlantillaCommand(int Id) : IRequest;

public class TogglePlantillaHandler : IRequestHandler<TogglePlantillaCommand>
{
    private readonly IPlantillaService _service;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public TogglePlantillaHandler(IPlantillaService service, IAuditoriaRepository auditoria, ICurrentUser currentUser)
    {
        _service = service;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(TogglePlantillaCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var before = await _service.GetByIdAsync(cmd.Id, ct);
        await _service.ToggleActivoAsync(cmd.Id, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "AlternarPlantillaNumeracion",
            "PlantillaNumeracion",
            before.Id.ToString(),
            NumeracionAuditDetails.PlantillaActiveChanged(before, !before.Activo)));
    }
}
