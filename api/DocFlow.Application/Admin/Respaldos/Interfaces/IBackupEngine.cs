namespace DocFlow.Application.Admin.Respaldos.Interfaces;

public interface IBackupEngine
{
    Task<BackupResult> ExecuteAsync(string outputFilePath, CancellationToken ct);
}

public record BackupResult(bool Success, long BytesEscritos, string? ErrorMessage);
