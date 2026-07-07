using System.Globalization;

namespace GestionCommerciale.Modules.Production.Services;

public static class ProductionQualityScore
{
    public static decimal ComputeScoreMortalite(decimal tauxMortalite) =>
        Math.Max(0m, 100m - tauxMortalite);

    public static decimal ComputeScoreAgrandissement(int? jours, int maxJours)
    {
        if (jours is not int d)
            return 0m;

        if (maxJours <= 0)
            return 100m;

        var clamped = Math.Min(d, maxJours);
        return 100m * (1m - (decimal)clamped / maxJours);
    }

    public static decimal? ComputeFacteurQualite(
        decimal tauxMortalite,
        int? dureeJours,
        int importanceMortalitePercent,
        int importanceAgrandissementPercent,
        int agrandissementMaxJours,
        bool estTerminee)
    {
        if (!estTerminee)
            return null;

        var wM = importanceMortalitePercent / 100m;
        var wA = importanceAgrandissementPercent / 100m;
        var scoreM = ComputeScoreMortalite(tauxMortalite);
        var scoreA = ComputeScoreAgrandissement(dureeJours, agrandissementMaxJours);
        return wM * scoreM + wA * scoreA;
    }

    public static string FormatFacteurLabel(decimal? facteur) =>
        facteur is decimal f
            ? $"{f.ToString("N0", CultureInfo.CurrentCulture)}%"
            : "—";
}
