using DocFlow.Domain.Enums;

namespace DocFlow.Application.Admin.Usuarios.DTOs;

public record UsuarioAdminDto(
    Guid Id,
    string NombreCompleto,
    string Email,
    string Rol,
    Guid? DepartamentoId,
    string? DepartamentoNombre,
    bool Activo,
    DateTime CreadoEn,
    string? Rut = null,
    string? RolId = null,
    bool EsCuentaPropia = false,
    bool EsUltimoAdminActivo = false,
    string? Usucod = null,
    string? Nombres = null,
    string? ApellidoPaterno = null,
    string? ApellidoMaterno = null,
    string? Telefono = null,
    string? Direccion = null,
    bool EstaBloqueado = false,
    DateTime? BloqueadoHasta = null
);
