using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeCorfor;

public record EliminarSeCorforCommand(string CorrTip) : IRequest;

public class EliminarSeCorforCommandHandler : IRequestHandler<EliminarSeCorforCommand>
{
    private readonly ISeCorforRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeCorforCommandHandler> _logger;

    public EliminarSeCorforCommandHandler(
        ISeCorforRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeCorforCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeCorforCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.CorrTip)
            ?? throw new KeyNotFoundException($"Correlativo {cmd.CorrTip} no encontrado.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeCorfor",
            "SeCorfor",
            entity.CorrTip,
            $"Correlativo eliminado: {entity.CorrDes}"));
    }
}
