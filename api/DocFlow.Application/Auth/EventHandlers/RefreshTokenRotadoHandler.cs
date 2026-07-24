using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class RefreshTokenRotadoHandler : INotificationHandler<RefreshTokenRotadoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public RefreshTokenRotadoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(RefreshTokenRotadoEvent evt, CancellationToken ct)
    {
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "RefreshTokenRotado",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"Refresh token rotado. TokenExpirado={evt.TokenExpirado}");
        await _auditoria.AddAsync(registro);
    }
}
