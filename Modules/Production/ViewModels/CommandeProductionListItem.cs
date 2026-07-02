using System.Globalization;

namespace GestionCommerciale.Modules.Production.ViewModels;

public sealed class CommandeProductionListItem
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public string FournisseurNom { get; init; } = string.Empty;
    public DateTime DateCommande { get; init; }
    public string CategorieCommandeNom { get; init; } = string.Empty;
    public string TypeNaissainNom { get; init; } = string.Empty;
    public int QuantiteNaissain { get; init; }
    public decimal TauxMortalite { get; init; }
    public bool EstTerminee { get; init; }
    public DateTime? DateExpiration { get; init; }
    public int OperationCount { get; init; }
    public int TotalHuitres { get; init; }
    public DateTime? LastOperationAt { get; init; }

    public string DateLabel => DateCommande.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
    public string DateExpirationLabel => DateExpiration?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";
    public string QuantiteNaissainLabel => QuantiteNaissain.ToString("N0", CultureInfo.CurrentCulture);
    public string TauxMortaliteLabel => ProductionOperation.FormatTauxMortaliteLabel(TauxMortalite);
    public string OperationCountLabel => OperationCount.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalHuitresLabel => TotalHuitres.ToString("N0", CultureInfo.CurrentCulture);
    public string LastOperationLabel => LastOperationAt?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";

    public bool ShowMortalite => EstTerminee;

    public string EtatLabel { get; set; } = string.Empty;
    public string NaissainChipPrefix { get; set; } = string.Empty;
    public string MortaliteChipLabel { get; set; } = string.Empty;
    public string OperationsChipLabel { get; set; } = string.Empty;
    public string TotalHuitresChipLabel { get; set; } = string.Empty;
    public string DerniereChipLabel { get; set; } = string.Empty;

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

    public string SummaryLine2 { get; set; } = string.Empty;
    public string SummaryLine3 { get; set; } = string.Empty;
}
