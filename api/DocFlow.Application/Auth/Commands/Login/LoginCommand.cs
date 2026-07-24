using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Exceptions;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.Login;

public record LoginCommand(string Identifier, string Password, string? MfaCode = null) : IRequest<LoginResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly ITotpService _totpService;
    private readonly ISecurityPolicyService _securityPolicy;
    private readonly IMfaSecretProtector _protector;

    public LoginCommandHandler(
        ISeUsuariRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IMediator mediator,
        ICurrentUser currentUser,
        ITotpService totpService,
        ISecurityPolicyService securityPolicy,
        IMfaSecretProtector protector)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _mediator = mediator;
        _currentUser = currentUser;
        _totpService = totpService;
        _securityPolicy = securityPolicy;
        _protector = protector;
    }

    public async Task<LoginResultDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var usuario = await _usuarioRepository.GetByIdentifierAsync(command.Identifier, ct);

        if (usuario is null)
        {
            await _mediator.Publish(new LoginFallidoEvent(
                Guid.Empty, command.Identifier, "Identificador inexistente",
                _currentUser.IpAddress, _currentUser.UserAgent, 0), ct);
            throw new UnauthorizedAccessException("Email o contraseña incorrectos.");
        }

        if (usuario.Personal is null)
            throw new UnauthorizedAccessException("No se pudo cargar el perfil de la cuenta.");

        var lockoutMaxAttempts = _securityPolicy.GetLockoutMaxAttempts();
        var lockoutDurationMinutes = _securityPolicy.GetLockoutDurationMinutes();

        // Check account lockout
        if (usuario.EstaBloqueado() || usuario.Personal is null || !usuario.Personal.Estado)
            throw new LoginFailedException($"La cuenta está bloqueada. Intente nuevamente en {lockoutDurationMinutes} minutos.", 0);

        if (!VerificarPassword(command.Password, usuario))
        {
            // Increment failed attempts and potentially lock account
            usuario.RegistrarIntentoFallido(lockoutMaxAttempts, lockoutDurationMinutes);
            if (usuario.IntentosFallidos >= lockoutMaxAttempts)
            {
                await _mediator.Publish(new UsuarioBloqueadoEvent(usuario.UsuarioId, lockoutDurationMinutes, usuario.IntentosFallidos, "auto"), ct);
            }

            var intentosRestantes = Math.Max(0, lockoutMaxAttempts - usuario.IntentosFallidos);
            await _usuarioRepository.UpdateAsync(usuario, ct);
            await _mediator.Publish(new LoginFallidoEvent(
                usuario.UsuarioId, command.Identifier, "Contraseña incorrecta",
                _currentUser.IpAddress, _currentUser.UserAgent, intentosRestantes), ct);
            throw new LoginFailedException("Identificador o contraseña incorrectos.", intentosRestantes);
        }

        if (!usuario.EstadoCuenta || usuario.Personal is null || !usuario.Personal.Estado)
            throw new UnauthorizedAccessException("Identificador o contraseña incorrectos.");

        // Login success — reset lockout counters
        usuario.Desbloquear();

        var requiresMandatorySetup = MandatoryMfaPolicyEvaluator.RequiresSetup(usuario, _securityPolicy);
        if (requiresMandatorySetup)
        {
            var setupToken = _jwtProvider.GenerateMfaToken(usuario.UsuarioId);
            usuario.RegistrarAcceso();
            await _usuarioRepository.UpdateAsync(usuario, ct);

            var dbPermisosSetup = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
            var setupUser = AuthUserMapper.ToDto(
                usuario,
                dbPermisos: dbPermisosSetup,
                lockoutMaxAttempts: lockoutMaxAttempts,
                authState: AuthState.MfaSetupRequired,
                setupToken: setupToken);

            return new LoginResultDto(string.Empty, string.Empty, 0, setupUser, AuthState.MfaSetupRequired, setupToken, true);
        }

        // Check MFA — if enabled, verify inline when mfaCode provided or throw challenge
        if (usuario.MfaEnabled)
        {
            if (!string.IsNullOrEmpty(command.MfaCode))
            {
                // Inline MFA verification
                if (string.IsNullOrEmpty(usuario.MfaSecretKey) ||
                    !_totpService.ValidateCode(_protector.Unprotect(usuario.MfaSecretKey), command.MfaCode))
                {
                    throw new UnauthorizedAccessException("Código de verificación inválido.");
                }

                // MFA passed — issue full tokens with mfa claim
                usuario.RegistrarAcceso();
                var mfaTokens = _jwtProvider.GenerateTokens(usuario, usuario.Personal!, mfaCompleted: true);
                var mfaExpiresIn = (int)(mfaTokens.expiresAt - DateTime.UtcNow).TotalSeconds;
                usuario.SetRefreshToken(mfaTokens.refreshToken, DateTime.UtcNow.AddDays(_jwtProvider.RefreshTokenExpirationDays));
                await _usuarioRepository.UpdateAsync(usuario, ct);

                await _mediator.Publish(new UsuarioAutenticadoEvent(usuario.UsuarioId, _currentUser.IpAddress ?? "unknown", "mfa"), ct);

                var dbPermisosMfa = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
                return new LoginResultDto(mfaTokens.accessToken, mfaTokens.refreshToken, mfaExpiresIn, AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisosMfa, lockoutMaxAttempts: lockoutMaxAttempts));
            }

            // No mfaCode — issue MFA challenge
            var mfaToken = _jwtProvider.GenerateMfaToken(usuario.UsuarioId);
            usuario.RegistrarAcceso();
            await _usuarioRepository.UpdateAsync(usuario, ct);
            throw new MfaRequiredException(mfaToken);
        }

        usuario.RegistrarAcceso();

        var tokens = _jwtProvider.GenerateTokens(usuario, usuario.Personal!);

        var expiresIn = (int)(tokens.expiresAt - DateTime.UtcNow).TotalSeconds;

        usuario.SetRefreshToken(tokens.refreshToken, DateTime.UtcNow.AddDays(_jwtProvider.RefreshTokenExpirationDays));
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new UsuarioAutenticadoEvent(usuario.UsuarioId, _currentUser.IpAddress ?? "unknown", "password"), ct);

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        return new LoginResultDto(
            tokens.accessToken,
            tokens.refreshToken,
            expiresIn,
            AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos, lockoutMaxAttempts: lockoutMaxAttempts));
    }

    private bool VerificarPassword(string password, SeUsuari usuario)
    {
        var passwordHash = usuario.PasswordHash;

        // Solo se aceptan hashes BCrypt. Las contraseñas legacy en texto plano fueron
        // migradas a BCrypt (DataSeeder.SeedSystemAsync); un hash no-BCrypt se rechaza.
        if (string.IsNullOrEmpty(passwordHash) || !passwordHash.StartsWith("$2"))
            return false;

        return _passwordHasher.Verify(password, passwordHash);
    }
}
