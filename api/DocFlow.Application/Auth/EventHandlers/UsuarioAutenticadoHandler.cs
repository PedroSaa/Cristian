using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.EventHandlers;

public class UsuarioAutenticadoHandler : INotificationHandler<UsuarioAutenticadoEvent>
{
    private readonly IAuditoriaRepository _auditoria;

    public UsuarioAutenticadoHandler(IAuditoriaRepository auditoria)
    {
        _auditoria = auditoria;
    }

    public async Task Handle(UsuarioAutenticadoEvent evt, CancellationToken ct)
    {
        var registro = RegistroAuditoria.Crear(
            evt.UsuarioId,
            "Login",
            "Usuario",
            evt.UsuarioId.ToString(),
            $"Usuario autenticado vía {evt.Metodo} desde {evt.Ip}");
        await _auditoria.AddAsync(registro);
    }
}
