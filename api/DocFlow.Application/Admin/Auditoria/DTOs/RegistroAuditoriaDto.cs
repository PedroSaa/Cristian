namespace DocFlow.Application.Admin.Auditoria.DTOs;

public record RegistroAuditoriaDto(
    Guid Id,
    Guid UsuarioId,
    string? UsuarioNombre,
    string Accion,
    string Entidad,
    string EntidadId,
    string? Detalle,
    string? DireccionIp,
    string? UserAgent,
    DateTime CreadoEn
);
