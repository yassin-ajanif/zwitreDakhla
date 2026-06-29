using GestionCommerciale.Shared.Helpers;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace GestionCommerciale.Shared.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private UpdateManager? _manager;

    private UpdateManager Manager => _manager ??= new UpdateManager(
        new GithubSource(AppInfo.GitHubRepoUrl, accessToken: null, prerelease: false));

    public bool IsUpdateSupported
    {
        get
        {
            try
            {
                return Manager.IsInstalled;
            }
            catch
            {
                return false;
            }
        }
    }

    public string CurrentVersion => AppInfo.Version;

    public VelopackAsset? PendingRestart
    {
        get
        {
            try
            {
                return IsUpdateSupported ? Manager.UpdatePendingRestart : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Manager.IsInstalled)
                return new AppUpdateCheckResult(AppUpdateStatus.NotInstalled);

            var info = await Manager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
            if (info is null)
                return new AppUpdateCheckResult(AppUpdateStatus.UpToDate);

            var asset = info.TargetFullRelease;
            var version = asset?.Version.ToString() ?? "?";
            var notes = asset?.NotesMarkdown;
            return new AppUpdateCheckResult(
                AppUpdateStatus.UpdateAvailable,
                new AppUpdateInfo(version, notes),
                info);
        }
        catch (NotInstalledException)
        {
            return new AppUpdateCheckResult(AppUpdateStatus.NotInstalled);
        }
        catch (Exception ex)
        {
            return new AppUpdateCheckResult(AppUpdateStatus.Error, ErrorMessage: ex.Message);
        }
    }

    public async Task DownloadAndApplyUpdatesAsync(
        UpdateInfo updateInfo,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Manager.IsInstalled)
            throw new InvalidOperationException("Application is not installed via Velopack.");

        await Manager.DownloadUpdatesAsync(updateInfo, p => progress?.Report(p), cancellationToken);
        Manager.ApplyUpdatesAndRestart(updateInfo);
    }

    public void ApplyPendingRestart(VelopackAsset asset)
    {
        if (!Manager.IsInstalled)
            throw new InvalidOperationException("Application is not installed via Velopack.");

        Manager.ApplyUpdatesAndRestart(asset);
    }
}
