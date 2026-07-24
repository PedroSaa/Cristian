using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class PasswordCambiadoHandler : INotificationHandler<PasswordCambiadoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public PasswordCambiadoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(PasswordCambiadoEvent evt, CancellationToken ct)
    {
        var detalle = evt.IniciadoPor == "admin"
            ? $"Contraseña cambiada por administrador ({evt.AdminId})"
            : "Contraseña cambiada por el usuario (propia)";

        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "PasswordCambiado",
            "Usuario",
            evt.UsuarioId.ToString(),
            detalle);
        await _auditoria.AddAsync(registro);
    }
}
