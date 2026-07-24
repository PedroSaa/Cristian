using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using MediatR;

namespace DocFlow.Application.Auth.Queries.GetPasswordPolicy;

public record GetPasswordPolicyQuery : IRequest<PasswordPolicyDto>;

public class GetPasswordPolicyQueryHandler : IRequestHandler<GetPasswordPolicyQuery, PasswordPolicyDto>
{
    private readonly ISecurityPolicyService _securityPolicy;

    public GetPasswordPolicyQueryHandler(ISecurityPolicyService securityPolicy)
    {
        _securityPolicy = securityPolicy;
    }

    public Task<PasswordPolicyDto> Handle(GetPasswordPolicyQuery query, CancellationToken ct)
    {
        // Lowercase y digit son piso fijo del backend (PasswordPolicyValidator siempre los exige);
        // el resto se lee de la política configurable, igual que la validación real.
        var dto = new PasswordPolicyDto(
            MinLength: _securityPolicy.GetPasswordMinLength(),
            RequireUppercase: _securityPolicy.GetPasswordRequireUpper(),
            RequireLowercase: true,
            RequireDigit: true,
            RequireSpecial: _securityPolicy.GetPasswordRequireSpecial());

        return Task.FromResult(dto);
    }
}
