using System.Globalization;
using ChargeEntity = GestionCommerciale.Modules.Charges.Models.Charge;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ChargeListRow
{
    public required ChargeEntity Charge { get; init; }
    public string CategorieNom { get; init; } = string.Empty;
    public string BeneficiaireNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string MontantLabel { get; init; } = string.Empty;
    public string LibellePreview { get; init; } = string.Empty;

    public static ChargeListRow Create(
        ChargeEntity charge,
        string categorieNom,
        string beneficiaireNom,
        string devise)
    {
        return new ChargeListRow
        {
            Charge = charge,
            CategorieNom = categorieNom,
            BeneficiaireNom = beneficiaireNom,
            DateShort = charge.Date.ToString("d", CultureInfo.CurrentCulture),
            MontantLabel = $"{charge.MontantTtc:N2} {devise}",
            LibellePreview = DocumentListFormat.NotePreview(charge.Libelle),
        };
    }
}
