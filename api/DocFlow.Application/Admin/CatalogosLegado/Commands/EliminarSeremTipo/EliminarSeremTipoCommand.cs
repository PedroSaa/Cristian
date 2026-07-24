using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.CatalogosLegado.Commands.EliminarSeremTipo;

public record EliminarSeremTipoCommand(string RemTipo) : IRequest;

public class EliminarSeremTipoCommandHandler : IRequestHandler<EliminarSeremTipoCommand>
{
    private readonly ISeremTipoRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EliminarSeremTipoCommandHandler> _logger;

    public EliminarSeremTipoCommandHandler(
        ISeremTipoRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<EliminarSeremTipoCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(EliminarSeremTipoCommand cmd, CancellationToken ct)
    {
        var tipo = await _repo.GetByIdAsync(cmd.RemTipo)
            ?? throw new KeyNotFoundException($"Tipo de remitente {cmd.RemTipo} no encontrado.");

        if (tipo.Serems.Count != 0)
            throw new InvalidOperationException("No se puede eliminar un tipo de remitente con remitentes asociados.");

        await _repo.DeleteAsync(tipo);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioId,
            "EliminarSeremTipo",
            "SeremTipo",
            tipo.RemTipo,
            $"Tipo de remitente eliminado: {tipo.RemDesc}"));
    }
}
