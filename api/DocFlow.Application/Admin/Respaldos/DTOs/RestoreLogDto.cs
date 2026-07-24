using DocFlow.Domain.Enums;

namespace DocFlow.Application.Admin.Respaldos.DTOs;

public record RestoreLogDto(
    Guid Id,
    Guid RespaldoId,
    DateTime FechaInicio,
    DateTime? FechaFin,
    EstadoRestore Estado,
    string? MensajeError
);
