namespace DocFlow.Application.Admin.Configuracion.DTOs;

public record ConfiguracionDto(
    Guid Id,
    string Clave,
    string Valor,
    string Descripcion,
    DateTime ActualizadoEn,
    string? Grupo = null,
    string? Tipo = null,
    int? MinValue = null,
    int? MaxValue = null);
