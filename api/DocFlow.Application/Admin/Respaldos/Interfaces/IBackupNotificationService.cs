namespace DocFlow.Application.Admin.Respaldos.Interfaces;

public interface IBackupNotificationService
{
    Task NotifyBackupFailedAsync(string backupName, string error);
    Task NotifyRestoreCompletedAsync(string backupName);
    Task NotifyRestoreFailedAsync(string backupName, string error);
}
