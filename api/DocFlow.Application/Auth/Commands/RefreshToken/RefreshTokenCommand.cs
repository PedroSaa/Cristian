using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.DomainEvents;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Falta la información para renovar la sesión.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly IMediator _mediator;
    private readonly ISecurityPolicyService _securityPolicy;

    public RefreshTokenCommandHandler(IJwtProvider jwtProvider, ISeUsuariRepository usuarioRepository, IMediator mediator, ISecurityPolicyService securityPolicy)
    {
        _jwtProvider = jwtProvider;
        _usuarioRepository = usuarioRepository;
        _mediator = mediator;
        _securityPolicy = securityPolicy;
    }

    public async Task<LoginResultDto> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var usuario = await _usuarioRepository.GetByRefreshTokenAsync(command.RefreshToken, ct);
        if (usuario is null)
            throw new UnauthorizedAccessException("La sesión no es válida.");

        if (usuario.Personal is null)
            throw new UnauthorizedAccessException("No se pudo cargar el perfil de la cuenta.");

        if (!usuario.EstadoCuenta || usuario.EstaBloqueado())
            throw new UnauthorizedAccessException("La cuenta está bloqueada o desactivada.");

        var tokenExpirado = usuario.RefreshTokenExpiry is null || usuario.RefreshTokenExpiry < DateTime.UtcNow;
        if (tokenExpirado)
            throw new UnauthorizedAccessException("La sesión expiró.");

        if (MandatoryMfaPolicyEvaluator.RequiresSetup(usuario, _securityPolicy))
        {
            var setupToken = _jwtProvider.GenerateMfaToken(usuario.UsuarioId);
            var dbPermisosSetup = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
            return new LoginResultDto(
                string.Empty,
                string.Empty,
                0,
                AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisosSetup, authState: AuthState.MfaSetupRequired, setupToken: setupToken),
                AuthState.MfaSetupRequired,
                setupToken,
                true);
        }

        // Rotar tokens: invalidar el actual y generar nuevos
        var tokens = _jwtProvider.GenerateTokens(usuario, usuario.Personal!, mfaCompleted: usuario.MfaEnabled);
        var expiresIn = (int)(tokens.expiresAt - DateTime.UtcNow).TotalSeconds;
        usuario.SetRefreshToken(tokens.refreshToken, DateTime.UtcNow.AddDays(_jwtProvider.RefreshTokenExpirationDays));
        await _usuarioRepository.UpdateAsync(usuario, ct);

        await _mediator.Publish(new RefreshTokenRotadoEvent(usuario.UsuarioId, false), ct);

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        return new LoginResultDto(
            tokens.accessToken,
            tokens.refreshToken,
            expiresIn,
            AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos));
    }
}
