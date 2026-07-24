namespace DocFlow.Application.Admin.Departamentos.DTOs;

public record DepartamentoAdminDto(
    Guid Id,
    string Nombre,
    string Codigo,
    bool Activo,
    DateTime CreadoEn,
    int TotalUsuarios);
