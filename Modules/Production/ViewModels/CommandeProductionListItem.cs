using System.Globalization;

namespace GestionCommerciale.Modules.Production.ViewModels;

public sealed class CommandeProductionListItem
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public string FournisseurNom { get; init; } = string.Empty;
    public DateTime DateCommande { get; init; }
    public string CategorieCommandeNom { get; init; } = string.Empty;
    public string TypeHuitreNom { get; init; } = string.Empty;
    public int QuantiteNaissain { get; init; }
    public decimal TauxMortalite { get; init; }
    public int? DureeAgrandissementJours { get; init; }
    public bool EstTerminee { get; init; }
    public DateTime? DateExpiration { get; init; }
    public int OperationCount { get; init; }
    public int TotalHuitres { get; init; }
    public int SumGrandHuitres { get; init; }
    public DateTime? LastOperationAt { get; init; }

    public int RestantOuMortesHuitres =>
        ProductionOperation.ComputeRemainingHuitresAtWater(QuantiteNaissain, SumGrandHuitres);

    public string RestantOuMortesHuitresLabel =>
        RestantOuMortesHuitres.ToString("N0", CultureInfo.CurrentCulture);

    public string DateLabel => DateCommande.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
    public string DateExpirationLabel => DateExpiration?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";
    public string QuantiteNaissainLabel => QuantiteNaissain.ToString("N0", CultureInfo.CurrentCulture);
    public string TauxMortaliteLabel => ProductionOperation.FormatTauxMortaliteLabel(TauxMortalite);
    public string TauxAgrandissementLabel =>
        ProductionOperation.FormatTauxAgrandissementLabel(DureeAgrandissementJours);
    public string OperationCountLabel => OperationCount.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalHuitresLabel => TotalHuitres.ToString("N0", CultureInfo.CurrentCulture);
    public string LastOperationLabel => LastOperationAt?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";

    public bool ShowMortalite => EstTerminee;
    public bool ShowAgrandissement => EstTerminee && DureeAgrandissementJours.HasValue;

    public string EtatLabel { get; set; } = string.Empty;
    public string NaissainChipPrefix { get; set; } = string.Empty;
    public string MortaliteChipLabel { get; set; } = string.Empty;
    public string AgrandissementChipLabel { get; set; } = string.Empty;
    public string OperationsChipLabel { get; set; } = string.Empty;
    public string WaterOrDeadHuitresChipLabel { get; set; } = string.Empty;
    public string ExpirationChipLabel { get; set; } = string.Empty;

    public bool ShowExpirationChip => EstTerminee && DateExpiration.HasValue;

    public string MortaliteBadgeBackground => TauxMortalite switch
    {
        >= 50m => "#FEF2F2",
        >= 30m => "#FFF7ED",
        _ => "#F0FDF4"
    };

    public string MortaliteBadgeBorder => TauxMortalite switch
    {
        >= 50m => "#FECACA",
        >= 30m => "#FDBA74",
        _ => "#86EFAC"
    };

    public string MortaliteBadgeForeground => TauxMortalite switch
    {
        >= 50m => "#DC2626",
        >= 30m => "#D97706",
        _ => "#16A34A"
    };

    public string AgrandissementBadgeBackground => DureeAgrandissementJours switch
    {
        <= 30 => "#F0FDF4",
        <= 60 => "#FFF7ED",
        _ => "#FEF2F2"
    };

    public string AgrandissementBadgeBorder => DureeAgrandissementJours switch
    {
        <= 30 => "#86EFAC",
        <= 60 => "#FDBA74",
        _ => "#FECACA"
    };

    public string AgrandissementBadgeForeground => DureeAgrandissementJours switch
    {
        <= 30 => "#16A34A",
        <= 60 => "#D97706",
        _ => "#DC2626"
    };

    public string SummaryLine2 { get; set; } = string.Empty;
    public string SummaryLine3 { get; set; } = string.Empty;
}
