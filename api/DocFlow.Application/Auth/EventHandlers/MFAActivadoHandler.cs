using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class MFAActivadoHandler : INotificationHandler<MFAActivadoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public MFAActivadoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(MFAActivadoEvent evt, CancellationToken ct)
    {
        var accion = evt.Accion == "activar" ? "activado" : "desactivado";
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "MFAActivado",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"MFA {accion} para el usuario");
        await _auditoria.AddAsync(registro);
    }
}
