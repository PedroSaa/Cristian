using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Unit>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(ISeUsuariRepository usuarioRepository, IMediator mediator, ICurrentUser currentUser)
    {
        _usuarioRepository = usuarioRepository;
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(LogoutCommand command, CancellationToken ct)
    {
        var usuario = await _usuarioRepository.GetByRefreshTokenAsync(command.RefreshToken, ct);
        if (usuario is null)
            return Unit.Value;

        usuario.RevokeAuthSessions();
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new SesionCerradaEvent(usuario.UsuarioId, _currentUser.IpAddress ?? "unknown"), ct);

        return Unit.Value;
    }
}
