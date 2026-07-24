using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Usuarios.Commands.DesactivarUsuario;

public record DesactivarUsuarioCommand(Guid Id) : IRequest;

public class DesactivarUsuarioCommandHandler : IRequestHandler<DesactivarUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DesactivarUsuarioCommandHandler> _logger;

    public DesactivarUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<DesactivarUsuarioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(DesactivarUsuarioCommand cmd, CancellationToken ct)
    {
        var usuario = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {cmd.Id}.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"No se encontró el perfil personal para el usuario con id {cmd.Id}.");

        var currentUserId = _currentUser.UserId;
        if (currentUserId.HasValue && currentUserId.Value == usuario.Id)
            throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");

        if (usuario.Activo && usuario.RolNombre == "Administrador")
        {
            var activeAdminCount = await _repo.CountActiveAdministratorsAsync(ct);
            if (activeAdminCount <= 1)
                throw new InvalidOperationException("No puedes desactivar al último administrador activo.");
        }

        personal.Desactivar();
        usuario.Desactivar();
        usuario.RevokeAuthSessions();
        await _repo.UpdateAsync(personal, usuario, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "DesactivarUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario desactivado: {personal.Correo}");
        await _auditoria.AddAsync(registro);
    }
}
