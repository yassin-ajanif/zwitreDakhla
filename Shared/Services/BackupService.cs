using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;

namespace GestionCommerciale.Shared.Services;

public sealed class BackupService : IBackupService
{
    public Task<int> GetBackupCountAsync(string backupDir, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupDir) || !Directory.Exists(backupDir))
            return Task.FromResult(0);

        var count = Directory.GetFiles(backupDir, "backup_*.db").Length;
        return Task.FromResult(count);
    }

    public async Task<string?> CreateBackupAsync(string backupDir, string? secondaryBackupDir = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupDir))
            return null;

        Directory.CreateDirectory(backupDir);

        var dbPath = Path.Combine(DatabasePath.GetDirectory(), "data.db");
        if (!File.Exists(dbPath))
            return null;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFile = Path.Combine(backupDir, $"backup_{timestamp}.db");

        try
        {
            await using var source = new SqliteConnection($"Data Source={dbPath}");
            await using var dest = new SqliteConnection($"Data Source={backupFile}");
            await source.OpenAsync(cancellationToken);
            await dest.OpenAsync(cancellationToken);
            source.BackupDatabase(dest);
        }
        catch
        {
            try { File.Delete(backupFile); } catch { }
            return null;
        }

        if (!string.IsNullOrWhiteSpace(secondaryBackupDir))
        {
            try
            {
                Directory.CreateDirectory(secondaryBackupDir);
                var secondaryFile = Path.Combine(secondaryBackupDir, Path.GetFileName(backupFile));
                File.Copy(backupFile, secondaryFile, overwrite: true);
            }
            catch
            {
                // Primary succeeded; secondary copy is best-effort.
            }
        }

        return backupFile;
    }

    public Task CleanupOldBackupsAsync(string backupDir, int retentionDays, string? secondaryBackupDir = null, CancellationToken cancellationToken = default)
    {
        CleanupOne(backupDir, retentionDays, cancellationToken);
        if (!string.IsNullOrWhiteSpace(secondaryBackupDir))
            CleanupOne(secondaryBackupDir, retentionDays, cancellationToken);
        return Task.CompletedTask;
    }

    private static void CleanupOne(string backupDir, int retentionDays, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupDir) || !Directory.Exists(backupDir) || retentionDays <= 0)
            return;

        var cutoff = DateTime.Now.AddDays(-retentionDays);

        foreach (var file in Directory.GetFiles(backupDir, "backup_*.db"))
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var lastWrite = File.GetLastWriteTime(file);
                if (lastWrite < cutoff)
                    File.Delete(file);
            }
            catch
            {
                // skip files we can't delete
            }
        }
    }
}
