using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSerem;

public record EliminarSeremCommand(string RemCod) : IRequest;

public class EliminarSeremCommandHandler : IRequestHandler<EliminarSeremCommand>
{
    private readonly ISeremRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeremCommandHandler> _logger;

    public EliminarSeremCommandHandler(
        ISeremRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeremCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeremCommand cmd, CancellationToken ct)
    {
        var serem = await _repo.GetByIdAsync(cmd.RemCod)
            ?? throw new KeyNotFoundException($"Remitente {cmd.RemCod} no encontrado.");

        await _repo.DeleteAsync(serem);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSerem",
            "Serem",
            serem.RemCod,
            $"Remitente eliminado: {serem.RemNomb}"));
    }
}
