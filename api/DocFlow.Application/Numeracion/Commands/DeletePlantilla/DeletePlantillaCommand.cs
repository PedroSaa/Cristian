using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.DeletePlantilla;

public record DeletePlantillaCommand(int Id) : IRequest;

public class DeletePlantillaHandler : IRequestHandler<DeletePlantillaCommand>
{
    private readonly IPlantillaService _service;
    private readonly IConfiguracionRepository _config;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public DeletePlantillaHandler(
        IPlantillaService service,
        IConfiguracionRepository config,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _service = service;
        _config = config;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePlantillaCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();

        // Guarda: no se puede eliminar la plantilla configurada como activa del sistema.
        var activa = await _config.GetByClaveAsync(NumeracionConfigKeys.PlantillaActiva);
        if (activa is not null && activa.Valor == cmd.Id.ToString())
            throw new InvalidOperationException(
                "No se puede eliminar la plantilla activa del sistema. Configurá otra plantilla como activa primero.");

        var before = await _service.GetByIdAsync(cmd.Id, ct); // lanza KeyNotFound si no existe
        await _service.EliminarAsync(cmd.Id, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "EliminarPlantillaNumeracion",
            "PlantillaNumeracion",
            cmd.Id.ToString(),
            $"Plantilla eliminada: id={before.Id}, descripcion={before.Descripcion}"));
    }
}
