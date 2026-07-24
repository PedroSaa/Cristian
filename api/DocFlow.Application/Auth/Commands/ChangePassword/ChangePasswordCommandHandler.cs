using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISeUsuariRepository _repo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMediator _mediator;

    public ChangePasswordHandler(ICurrentUser currentUser, ISeUsuariRepository repo, IPasswordHasher passwordHasher, IMediator mediator)
    {
        _currentUser = currentUser;
        _repo = repo;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
    }

    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var usuario = await _repo.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new KeyNotFoundException("No se pudo cargar la cuenta actual.");

        var currentHash = usuario.PasswordHash;
        if (currentHash == null || !_passwordHasher.Verify(cmd.CurrentPassword, currentHash))
            throw new InvalidOperationException("La contraseña actual ingresada no es correcta.");

        usuario.SetPassword(_passwordHasher.Hash(cmd.NewPassword));
        usuario.ClearRefreshToken();
        await _repo.UpdateAsync(usuario, ct);

        await _mediator.Publish(new PasswordCambiadoEvent(usuario.UsuarioId, "usuario", null), ct);
    }
}
