using System.Reflection;

namespace GestionCommerciale.Shared.Helpers;

public static class AppInfo
{
    public const string Name = "Huitres";

    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public static string WindowTitle => $"{Name} v{Version}";

    /// <summary>Velopack pack id (must match vpk --packId).</summary>
    public const string PackId = "Huitres";

    /// <summary>GitHub repository used for Velopack release updates.</summary>
    public const string GitHubRepoUrl = "https://github.com/yassin-ajanif/zwitreDakhla";
}
