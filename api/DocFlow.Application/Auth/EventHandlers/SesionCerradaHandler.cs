using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class SesionCerradaHandler : INotificationHandler<SesionCerradaEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public SesionCerradaHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(SesionCerradaEvent evt, CancellationToken ct)
    {
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "SesionCerrada",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"Sesión cerrada desde {evt.Ip}");
        await _auditoria.AddAsync(registro);
    }
}
