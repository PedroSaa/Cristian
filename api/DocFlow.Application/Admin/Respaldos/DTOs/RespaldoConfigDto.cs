namespace DocFlow.Application.Admin.Respaldos.DTOs;

public record RespaldoConfigDto(
    Guid Id,
    int IntervaloMinutos,
    bool Habilitado,
    int MaxBackupCount,
    int RetentionDays,
    string OutputPath,
    int TimeoutMinutos,
    DateTime ActualizadoEn
);
