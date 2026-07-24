using DocFlow.Domain.Authorization;
using DocFlow.Domain.Entities;

namespace DocFlow.Application.Common.Authorization;

public static class RolePolicyResolver
{
    public static RoleGroup ResolveGroup(Rol? rol)
    {
        if (rol is null)
            return RoleGroup.OtrosUsuarios;

        if (IsAdministratorCode(rol.CodigoSistema) || IsAdministratorCode(rol.Nombre) || rol.Grupo == RoleGroup.Administrador)
            return RoleGroup.Administrador;

        return RoleGroup.OtrosUsuarios;
    }

    private static bool IsAdministratorCode(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           string.Equals(value.Trim(), RoleCodes.Administrador, StringComparison.OrdinalIgnoreCase);
}
