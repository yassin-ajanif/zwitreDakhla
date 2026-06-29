using Velopack;

namespace GestionCommerciale.Shared.Services;

public enum AppUpdateStatus
{
    NotInstalled,
    UpToDate,
    UpdateAvailable,
    Error
}

public sealed record AppUpdateInfo(string Version, string? ReleaseNotes);

public sealed record AppUpdateCheckResult(
    AppUpdateStatus Status,
    AppUpdateInfo? Update = null,
    UpdateInfo? VelopackUpdate = null,
    string? ErrorMessage = null);

public interface IAppUpdateService
{
    bool IsUpdateSupported { get; }
    string CurrentVersion { get; }
    VelopackAsset? PendingRestart { get; }
    Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task DownloadAndApplyUpdatesAsync(UpdateInfo updateInfo, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    void ApplyPendingRestart(VelopackAsset asset);
}
