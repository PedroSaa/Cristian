using DocFlow.Domain.Enums;

namespace DocFlow.Application.Admin.Respaldos.DTOs;

public record RespaldoDto(
    Guid Id,
    string Nombre,
    DateTime FechaCreacion,
    long TamanioBytes,
    EstadoRespaldo Estado,
    string Ruta
);
