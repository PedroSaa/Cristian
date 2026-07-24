using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Mfa;

public record DisableMfaCommand(string CurrentPassword) : IRequest<Unit>;

public class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Debe ingresar su contraseña actual para desactivar la autenticación en dos pasos.");
    }
}

public class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand, Unit>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMediator _mediator;

    public DisableMfaCommandHandler(
        ISeUsuariRepository usuarioRepository,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        IMediator mediator)
    {
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(DisableMfaCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException();

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new UnauthorizedAccessException();

        if (!_passwordHasher.Verify(command.CurrentPassword, usuario.PasswordHash))
            throw new InvalidOperationException("La contraseña actual ingresada no es correcta.");

        usuario.EstablecerMfa(null);
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new MFAActivadoEvent(usuario.UsuarioId, "desactivar"), ct);

        return Unit.Value;
    }
}
