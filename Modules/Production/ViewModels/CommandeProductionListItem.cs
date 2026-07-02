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
    public int OperationCount { get; init; }
    public int TotalHuitres { get; init; }
    public DateTime? LastOperationAt { get; init; }

    public string DateLabel => DateCommande.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
    public string QuantiteNaissainLabel => QuantiteNaissain.ToString("N0", CultureInfo.CurrentCulture);
    public string TauxMortaliteLabel => TauxMortalite.ToString("N1", CultureInfo.CurrentCulture);
    public string OperationCountLabel => OperationCount.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalHuitresLabel => TotalHuitres.ToString("N0", CultureInfo.CurrentCulture);
    public string LastOperationLabel => LastOperationAt?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";

    public string SummaryLine2 { get; set; } = string.Empty;
    public string SummaryLine3 { get; set; } = string.Empty;
}
