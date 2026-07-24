using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Login;

public record LoginMfaCommand(string MfaToken, string Code) : IRequest<LoginResultDto>;

public class LoginMfaCommandValidator : AbstractValidator<LoginMfaCommand>
{
    public LoginMfaCommandValidator()
    {
        RuleFor(x => x.MfaToken)
            .NotEmpty().WithMessage("Falta el token de verificación.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de verificación es obligatorio.")
            .Length(6).WithMessage("El código debe tener 6 dígitos.");
    }
}

public class LoginMfaCommandHandler : IRequestHandler<LoginMfaCommand, LoginResultDto>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IMfaSecretProtector _protector;

    public LoginMfaCommandHandler(
        ISeUsuariRepository usuarioRepository,
        IJwtProvider jwtProvider,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        IMediator mediator,
        ICurrentUser currentUser,
        IMfaSecretProtector protector)
    {
        _usuarioRepository = usuarioRepository;
        _jwtProvider = jwtProvider;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _mediator = mediator;
        _currentUser = currentUser;
        _protector = protector;
    }

    public async Task<LoginResultDto> Handle(LoginMfaCommand command, CancellationToken ct)
    {
        var userId = _jwtProvider.ValidateMfaToken(command.MfaToken);
        if (userId is null)
            throw new UnauthorizedAccessException();

        var usuario = await _usuarioRepository.GetByIdAsync(userId.Value, ct)
            ?? throw new UnauthorizedAccessException();

        if (usuario.Personal is null)
            throw new UnauthorizedAccessException("No se pudo cargar el perfil de la cuenta.");

        if (!usuario.MfaEnabled || string.IsNullOrEmpty(usuario.MfaSecretKey))
            throw new UnauthorizedAccessException("La autenticación en dos pasos no está activada.");

        var isValid = _totpService.ValidateCode(_protector.Unprotect(usuario.MfaSecretKey), command.Code);
        if (!isValid)
            throw new UnauthorizedAccessException("Código de verificación inválido.");

        usuario.Desbloquear();
        usuario.RegistrarAcceso();

        var tokens = _jwtProvider.GenerateTokens(usuario, usuario.Personal!, mfaCompleted: true);
        var expiresIn = (int)(tokens.expiresAt - DateTime.UtcNow).TotalSeconds;

        usuario.SetRefreshToken(tokens.refreshToken, DateTime.UtcNow.AddDays(_jwtProvider.RefreshTokenExpirationDays));
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new UsuarioAutenticadoEvent(usuario.UsuarioId, _currentUser.IpAddress ?? "unknown", "mfa"), ct);

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        return new LoginResultDto(
            tokens.accessToken,
            tokens.refreshToken,
            expiresIn,
            AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos));
    }
}
