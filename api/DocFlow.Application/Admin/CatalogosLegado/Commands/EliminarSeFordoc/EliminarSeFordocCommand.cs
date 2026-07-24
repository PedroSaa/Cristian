using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeFordoc;

public record EliminarSeFordocCommand(short TipoCod) : IRequest;

public class EliminarSeFordocCommandHandler : IRequestHandler<EliminarSeFordocCommand>
{
    private readonly ISeFordocRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeFordocCommandHandler> _logger;

    public EliminarSeFordocCommandHandler(
        ISeFordocRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeFordocCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeFordocCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.TipoCod)
            ?? throw new KeyNotFoundException($"Formato de documento {cmd.TipoCod} no encontrado.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeFordoc",
            "SeFordoc",
            entity.TipoCod.ToString(),
            $"Formato de documento eliminado: {entity.TipoDesc}"));
    }
}
