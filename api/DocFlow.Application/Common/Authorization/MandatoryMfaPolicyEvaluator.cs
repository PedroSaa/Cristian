using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;

namespace DocFlow.Application.Common.Authorization;

public static class MandatoryMfaPolicyEvaluator
{
    public static AuthState ResolveAuthState(SeUsuari usuario, ISecurityPolicyService securityPolicy)
        => RequiresSetup(usuario, securityPolicy) ? AuthState.MfaSetupRequired : AuthState.Normal;

    public static bool RequiresSetup(SeUsuari usuario, ISecurityPolicyService securityPolicy)
    {
        if (usuario.MfaEnabled)
            return false;

        return RolePolicyResolver.ResolveGroup(usuario.Rol) == RoleGroup.Administrador
            ? securityPolicy.IsMfaRequiredForAdministrators()
            : securityPolicy.IsMfaRequiredForOtherUsers();
    }
}
