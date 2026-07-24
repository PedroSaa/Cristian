using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeTiptar;

public record EliminarSeTiptarCommand(string DFTACCION) : IRequest;

public class EliminarSeTiptarCommandHandler : IRequestHandler<EliminarSeTiptarCommand>
{
    private readonly ISeTiptarRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeTiptarCommandHandler> _logger;

    public EliminarSeTiptarCommandHandler(
        ISeTiptarRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeTiptarCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeTiptarCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.DFTACCION)
            ?? throw new KeyNotFoundException($"Acción de tarea {cmd.DFTACCION} no encontrada.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeTiptar",
            "SeTiptar",
            entity.DFTACCION,
            $"Acción de tarea eliminada: {entity.DFTACCION}"));
    }
}
