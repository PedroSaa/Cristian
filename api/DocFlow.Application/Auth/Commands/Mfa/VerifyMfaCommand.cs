using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Mfa;

public record VerifyMfaCommand(string Code) : IRequest<MfaVerificationResult>;

public class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .Length(6).WithMessage("El código debe tener 6 dígitos.");
    }
}

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, MfaVerificationResult>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly ITotpService _totpService;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly IMfaSecretProtector _protector;

    public VerifyMfaCommandHandler(
        ISeUsuariRepository usuarioRepository,
        ITotpService totpService,
        ICurrentUser currentUser,
        IMediator mediator,
        IMfaSecretProtector protector)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
        _currentUser = currentUser;
        _mediator = mediator;
        _protector = protector;
    }

    public async Task<MfaVerificationResult> Handle(VerifyMfaCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException();

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new UnauthorizedAccessException();

        if (string.IsNullOrEmpty(usuario.MfaSecretKey))
            throw new InvalidOperationException("Primero debe activar la autenticación en dos pasos.");

        var isValid = _totpService.ValidateCode(_protector.Unprotect(usuario.MfaSecretKey), command.Code);

        if (!isValid)
            return new MfaVerificationResult(false, "Código de verificación inválido.");

        usuario.EstablecerMfa(usuario.MfaSecretKey);
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new MFAActivadoEvent(usuario.UsuarioId, "activar"), ct);

        return new MfaVerificationResult(true, null);
    }
}
