using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeFormaEnvio;

public record EliminarSeFormaEnvioCommand(short IdFormaEnvio) : IRequest;

public class EliminarSeFormaEnvioCommandHandler : IRequestHandler<EliminarSeFormaEnvioCommand>
{
    private readonly ISeFormaEnvioRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeFormaEnvioCommandHandler> _logger;

    public EliminarSeFormaEnvioCommandHandler(
        ISeFormaEnvioRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeFormaEnvioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeFormaEnvioCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.IdFormaEnvio)
            ?? throw new KeyNotFoundException($"Forma de envío {cmd.IdFormaEnvio} no encontrada.");

        await _repo.DeleteAsync(entity);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeFormaEnvio",
            "SeFormaEnvio",
            entity.IdFormaEnvio.ToString(),
            $"Forma de envío eliminada: {entity.FormaEnvio}"));
    }
}
