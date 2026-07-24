using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class LoginFallidoHandler : INotificationHandler<LoginFallidoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public LoginFallidoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(LoginFallidoEvent evt, CancellationToken ct)
    {
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "LoginFallido",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"Intento de login fallido para '{evt.Identificador}': {evt.Motivo}. Intentos restantes: {evt.IntentosRestantes}.",
            evt.IpAddress,
            evt.UserAgent);
        await _auditoria.AddAsync(registro);
    }
}
