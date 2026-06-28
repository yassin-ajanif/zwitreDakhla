using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Shared.Services;

public sealed class LicenseService : ILicenseService
{
    private static string ComputeExpectedKey() =>
        $"{DateTime.Today.Day}{DateTime.Today.Month}{DateTime.Today.Year}";

    public bool IsTrialActive(AppSettingsRow settings)
    {
        if (IsLicensed(settings)) return false;
        if (settings.TrialStartedAt is null) return false;
        return settings.TrialStartedAt.Value.AddDays(3) >= DateTime.UtcNow;
    }

    public bool IsLicensed(AppSettingsRow settings) =>
        settings.LicenseKey is not null && settings.LicenseKey == ComputeExpectedKey();

    public bool IsTrialExpired(AppSettingsRow settings)
    {
        if (IsLicensed(settings)) return false;
        if (settings.TrialStartedAt is null) return false;
        return settings.TrialStartedAt.Value.AddDays(3) < DateTime.UtcNow;
    }

    public bool ValidateLicenseKey(string key) =>
        key == ComputeExpectedKey();
}
