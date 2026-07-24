using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocFlow.Application.Admin.Usuarios.Commands.ActivarUsuario;

public record ActivarUsuarioCommand(Guid Id) : IRequest;

public class ActivarUsuarioCommandHandler : IRequestHandler<ActivarUsuarioCommand>
{
    private readonly IUsuarioAdminRepository _repo;
    private readonly IAuditoriaRepository _auditoria;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActivarUsuarioCommandHandler> _logger;

    public ActivarUsuarioCommandHandler(
        IUsuarioAdminRepository repo,
        IAuditoriaRepository auditoria,
        ICurrentUser currentUser,
        ILogger<ActivarUsuarioCommandHandler> logger)
    {
        _repo = repo;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(ActivarUsuarioCommand cmd, CancellationToken ct)
    {
        var usuario = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con id {cmd.Id}.");

        var personal = usuario.Personal
            ?? throw new KeyNotFoundException($"No se encontró el perfil personal para el usuario con id {cmd.Id}.");

        personal.Activar();
        usuario.Activar();
        await _repo.UpdateAsync(personal, usuario, ct);

        var usuarioId = _currentUser.RequireAuthenticatedUserId();
        var registro = RegistroAuditoria.Crear(
            usuarioId,
            "ActivarUsuario",
            "Usuario",
            usuario.Id.ToString(),
            $"Usuario activado: {personal.Correo}");
        await _auditoria.AddAsync(registro);
    }
}
