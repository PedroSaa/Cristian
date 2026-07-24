using DocFlow.Application.Admin.Permisos.DTOs;

namespace DocFlow.Application.Admin.Roles.DTOs;

public record RolDto(Guid Id, string Nombre, string? Descripcion, bool EsSistema,
    IReadOnlyList<PermisoDto>? Permisos = null);
