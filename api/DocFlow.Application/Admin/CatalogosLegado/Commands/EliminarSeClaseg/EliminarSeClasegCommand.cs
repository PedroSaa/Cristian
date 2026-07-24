using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeClaseg;

public record EliminarSeClasegCommand(short DFClasif) : IRequest;

public class EliminarSeClasegCommandHandler : IRequestHandler<EliminarSeClasegCommand>
{
    private readonly ISeClasegRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeClasegCommandHandler> _logger;

    public EliminarSeClasegCommandHandler(
        ISeClasegRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeClasegCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeClasegCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.DFClasif)
            ?? throw new KeyNotFoundException($"Clasificación {cmd.DFClasif} no encontrada.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeClaseg",
            "SeClaseg",
            entity.DFClasif.ToString(),
            $"Clasificación eliminada: {entity.DFDClasif}"));
    }
}
