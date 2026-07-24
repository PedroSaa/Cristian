using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeForpla;

public record EliminarSeForplaCommand(string CodForm) : IRequest;

public class EliminarSeForplaCommandHandler : IRequestHandler<EliminarSeForplaCommand>
{
    private readonly ISeForplaRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeForplaCommandHandler> _logger;

    public EliminarSeForplaCommandHandler(
        ISeForplaRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeForplaCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeForplaCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.CodForm)
            ?? throw new KeyNotFoundException($"Plantilla {cmd.CodForm} no encontrada.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeForpla",
            "SeForpla",
            entity.CodForm,
            $"Plantilla eliminada: {entity.NomForm}"));
    }
}
