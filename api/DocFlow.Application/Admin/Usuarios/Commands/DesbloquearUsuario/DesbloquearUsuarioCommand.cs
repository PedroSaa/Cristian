using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Commands.DesbloquearUsuario;

public record DesbloquearUsuarioCommand(Guid UsuarioId) : IRequest;

public class DesbloquearUsuarioCommandHandler : IRequestHandler<DesbloquearUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public DesbloquearUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(DesbloquearUsuarioCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var usuario = await _repo.GetByIdAsync(cmd.UsuarioId, ct)
            ?? throw new KeyNotFoundException($"Usuario {cmd.UsuarioId} no encontrado.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"Perfil personal del usuario {cmd.UsuarioId} no encontrado.");

        usuario.Desbloquear();
        await _repo.UpdateAsync(personal, usuario, ct);

        var registro = RegistroAuditoria.Crear(
            actorId,
            "DesbloquearUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario desbloqueado: {personal.Correo}");
        await _auditoria.AddAsync(registro);
    }
}
