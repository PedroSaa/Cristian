namespace DocFlow.Infrastructure.Configuration;

public class BackupSettings
{
    public const string SectionName = "BackupSettings";

    public string OutputPath { get; init; } = "./Respaldos";
    public int TimeoutSeconds { get; init; } = 300;
    public int MaxBackupCount { get; init; } = 10;
    public int RetentionDays { get; init; } = 30;
    public string Provider { get; init; } = "PostgreSQL";
    public string? DatabaseName { get; set; }
    public string? PgDumpPath { get; set; }
    public string? PgRestorePath { get; set; }
    public string? PgHost { get; set; }
    public int PgPort { get; set; } = 5432;
    public string? PgUsername { get; set; }
    public string? PgPassword { get; set; }
}
