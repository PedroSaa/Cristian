using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class UsuarioBloqueadoHandler : INotificationHandler<UsuarioBloqueadoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public UsuarioBloqueadoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(UsuarioBloqueadoEvent evt, CancellationToken ct)
    {
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "CuentaBloqueada",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"Cuenta bloqueada por {evt.DuracionMinutos} minutos tras {evt.IntentosFallidos} intentos fallidos (origen: {evt.Origen})");
        await _auditoria.AddAsync(registro);
    }
}
