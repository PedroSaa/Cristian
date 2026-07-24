using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UsuarioDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UsuarioDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISeUsuariRepository _usuarioRepository;
    private readonly ISecurityPolicyService _securityPolicy;

    public GetCurrentUserQueryHandler(ICurrentUser currentUser, ISeUsuariRepository usuarioRepository, ISecurityPolicyService securityPolicy)
    {
        _currentUser = currentUser;
        _usuarioRepository = usuarioRepository;
        _securityPolicy = securityPolicy;
    }

    public async Task<UsuarioDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("Debe iniciar sesión para continuar.");

        var usuario = await _usuarioRepository.GetByIdAsync(_currentUser.UserId.Value, ct);
        if (usuario is null)
            throw new UnauthorizedAccessException("No se pudo cargar la cuenta actual.");

        var dbPermisos = usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).ToList() as IReadOnlyList<Permiso>;
        var authState = MandatoryMfaPolicyEvaluator.ResolveAuthState(usuario, _securityPolicy);
        return AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos, authState: authState);
    }
}
