namespace DocFlow.Application.Admin.Permisos.DTOs;

public record PermisoDto(Guid Id, string Nombre, string? Descripcion, string Grupo);
