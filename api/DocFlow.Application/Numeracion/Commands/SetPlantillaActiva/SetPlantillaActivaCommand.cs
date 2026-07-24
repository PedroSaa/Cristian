using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Commands.SetPlantillaActiva;

/// <summary>Define la plantilla de numeración que usa el sistema (única activa).</summary>
public record SetPlantillaActivaCommand(int Id) : IRequest;

public class SetPlantillaActivaHandler : IRequestHandler<SetPlantillaActivaCommand>
{
    private readonly IPlantillaService _service;
    private readonly IConfiguracionRepository _config;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public SetPlantillaActivaHandler(
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

    public async Task Handle(SetPlantillaActivaCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();

        // Marca la elegida como única activa (lanza si no existe).
        await _service.SetActivaAsync(cmd.Id, ct);

        // Persiste la elección en configuración (fuente de verdad para el sistema).
        var existing = await _config.GetByClaveAsync(NumeracionConfigKeys.PlantillaActiva);
        if (existing is null)
        {
            await _config.UpsertAsync(ConfiguracionSistema.Crear(
                Guid.NewGuid(), NumeracionConfigKeys.PlantillaActiva, cmd.Id.ToString(),
                "Plantilla de numeración del sistema"));
        }
        else
        {
            existing.Actualizar(cmd.Id.ToString(), existing.Descripcion);
            await _config.UpsertAsync(existing);
        }

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            actorId,
            "SetPlantillaNumeracionActiva",
            "PlantillaNumeracion",
            cmd.Id.ToString(),
            $"Plantilla de numeración activa del sistema: {cmd.Id}"));
    }
}
