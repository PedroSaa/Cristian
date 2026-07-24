namespace DocFlow.Application.Admin.Integraciones.DTOs;

public record IntegracionTestResultDto(bool Success, string Mensaje, int? LatencyMs);
