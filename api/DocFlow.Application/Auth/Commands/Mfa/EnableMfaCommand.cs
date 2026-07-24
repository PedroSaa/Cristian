using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Mfa;

public record EnableMfaCommand() : IRequest<EnableMfaResult>;

public class EnableMfaCommandValidator : AbstractValidator<EnableMfaCommand>
{
    // Validation is handled by the handler — user must be authenticated
}

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, EnableMfaResult>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly ITotpService _totpService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IMfaSecretProtector _protector;

    public EnableMfaCommandHandler(
        ISeUsuariRepository usuarioRepository,
        ITotpService totpService,
        ICurrentUser currentUser,
        IAuditoriaRepository auditoria,
        IMfaSecretProtector protector)
    {
        _usuarioRepository = usuarioRepository;
        _totpService = totpService;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _protector = protector;
    }

    public async Task<EnableMfaResult> Handle(EnableMfaCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException();

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUser.UserId.Value, ct)
            ?? throw new UnauthorizedAccessException();

        var secret = _totpService.GenerateSecret();
        // Se persiste cifrado; el secreto en claro solo se devuelve para el QR/provisioning.
        usuario.EstablecerMfa(_protector.Protect(secret));
        await _usuarioRepository.UpdateAsync(usuario, ct);

        // IP/User-Agent los completa AuditoriaRepository de forma central.
        await _auditoria.AddAsync(RegistroAuditoria.Crear(
            usuario.UsuarioId, "MFASetupIniciado", "Usuario", usuario.UsuarioId.ToString(),
            "Inició la configuración de MFA (pendiente de confirmación)"));

        var provisioningUri = _totpService.GenerateProvisioningUri(secret, usuario.Personal?.Correo ?? string.Empty);

        return new EnableMfaResult(provisioningUri, secret);
    }
}
