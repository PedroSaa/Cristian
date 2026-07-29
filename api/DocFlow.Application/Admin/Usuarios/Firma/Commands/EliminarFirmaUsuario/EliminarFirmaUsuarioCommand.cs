using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Firma.Commands.EliminarFirmaUsuario;

/// <summary>Removes the signature configured for a user. Throws KeyNotFoundException (404) if none exists.</summary>
public record EliminarFirmaUsuarioCommand(Guid UsuarioId) : IRequest;

public class EliminarFirmaUsuarioHandler : IRequestHandler<EliminarFirmaUsuarioCommand>
{
    private readonly IFirmaUsuarioRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public EliminarFirmaUsuarioHandler(
        IFirmaUsuarioRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(EliminarFirmaUsuarioCommand cmd, CancellationToken ct)
    {
        var usuarioAuditoria = _currentUser.RequireAuthenticatedUserId();

        var firma = await _repo.GetByUsuarioAsync(cmd.UsuarioId, ct)
            ?? throw new KeyNotFoundException("El usuario no tiene una firma configurada.");

        await _repo.DeleteAsync(cmd.UsuarioId, ct);

        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuarioAuditoria,
            "FirmaUsuarioEliminada",
            nameof(FirmaUsuario),
            cmd.UsuarioId.ToString(),
            $"Firma eliminada para el usuario {cmd.UsuarioId}."));
    }
}
