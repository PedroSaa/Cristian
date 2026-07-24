using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Usuarios.Commands.BloquearUsuario;

public record BloquearUsuarioCommand(Guid UsuarioId) : IRequest;

public class BloquearUsuarioCommandHandler : IRequestHandler<BloquearUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;

    public BloquearUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task Handle(BloquearUsuarioCommand cmd, CancellationToken ct)
    {
        var actorId = _currentUser.RequireAuthenticatedUserId();
        var usuario = await _repo.GetByIdAsync(cmd.UsuarioId, ct)
            ?? throw new KeyNotFoundException($"Usuario {cmd.UsuarioId} no encontrado.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"Perfil personal del usuario {cmd.UsuarioId} no encontrado.");

        if (actorId == usuario.Id)
            throw new InvalidOperationException("No puedes bloquear tu propia cuenta.");

        if (usuario.Activo && usuario.RolNombre == "Administrador")
        {
            var activeAdminCount = await _repo.CountActiveAdministratorsAsync(ct);
            if (activeAdminCount <= 1)
                throw new InvalidOperationException("No puedes bloquear al último administrador activo.");
        }

        usuario.Bloquear();
        usuario.RevokeAuthSessions();
        await _repo.UpdateAsync(personal, usuario, ct);

        var registro = RegistroAuditoria.Crear(
            actorId,
            "BloquearUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario bloqueado: {personal.Correo}");
        await _auditoria.AddAsync(registro);
    }
}
