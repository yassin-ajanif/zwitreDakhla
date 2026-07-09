using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.RegularExpressions;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Stock.Models;

public class MouvementStock : BaseEntity
{
    private static readonly Regex TrailingDateTimeRegex = new(
        @"\s(\d{2}/\d{2}/\d{4} \d{2}:\d{2})$",
        RegexOptions.Compiled);

    private static readonly Regex CommandeNumeroRegex = new(
        @"\bCMD-\d{4}-\d{4}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public int ProduitId { get; set; }
    public Produit? Produit { get; set; }
    public TypeMouvement Type { get; set; }
    public decimal StockAvant { get; set; }
    public decimal Quantite { get; set; }

    public string OrigineType { get; set; } = string.Empty;
    public int? OrigineId { get; set; }
    public string Note { get; set; } = string.Empty;

    [NotMapped]
    public decimal StockApres => Type switch
    {
        TypeMouvement.Entree => StockAvant + Quantite,
        TypeMouvement.Sortie => StockAvant - Quantite,
        TypeMouvement.Ajustement => StockAvant + Quantite,
        _ => StockAvant
    };

    [NotMapped]
    public decimal SignedQuantite => Type switch
    {
        TypeMouvement.Sortie => -Math.Abs(Quantite),
        TypeMouvement.Entree => Math.Abs(Quantite),
        TypeMouvement.Ajustement => Quantite,
        _ => Quantite
    };

    [NotMapped]
    public string QuantiteSignedLabel
    {
        get
        {
            var signed = SignedQuantite;
            var formatted = Math.Abs(signed).ToString("N0", CultureInfo.CurrentCulture);
            return signed >= 0 ? $"+{formatted}" : $"-{formatted}";
        }
    }

    [NotMapped]
    public string PartyName { get; set; } = string.Empty;

    [NotMapped]
    public bool PartyIsSupplier { get; set; }

    [NotMapped]
    public bool HasPartyName => !string.IsNullOrWhiteSpace(PartyName);

    [NotMapped]
    public decimal PartyColorSignal => PartyIsSupplier ? 1m : -1m;

    [NotMapped]
    public string DocumentRef => string.IsNullOrWhiteSpace(Note) ? OrigineType : Note;

    [NotMapped]
    public string DocumentTitle
    {
        get
        {
            var text = DocumentRef;
            var match = TrailingDateTimeRegex.Match(text);
            return match.Success ? text[..match.Index].TrimEnd() : text;
        }
    }

    [NotMapped]
    public string DocumentDateTime
    {
        get
        {
            var match = TrailingDateTimeRegex.Match(DocumentRef);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }

    [NotMapped]
    public bool HasDocumentDateTime => DocumentDateTime.Length > 0;

    [NotMapped]
    public bool ShowDocumentDateTime =>
        HasDocumentDateTime && !(OrigineType == "Production" && HasCommandeChip);

    [NotMapped]
    public string UnitPriceDetail { get; set; } = string.Empty;

    [NotMapped]
    public bool HasUnitPriceDetail => !string.IsNullOrEmpty(UnitPriceDetail);

    [NotMapped]
    public bool ShowAdjustmentDetail =>
        Type == TypeMouvement.Ajustement && !string.IsNullOrWhiteSpace(NoteBody);

    // ---- Note parsing (source of truth) ----

    [NotMapped]
    public string StatusLabel
    {
        get
        {
            var title = DocumentTitle;
            if (title.StartsWith("Annulation ", StringComparison.Ordinal))
                return "Annulation";
            if (title.StartsWith("Modification ", StringComparison.Ordinal))
                return "Modification";
            if (title.StartsWith("Modif ", StringComparison.Ordinal))
                return "Modification";
            return string.Empty;
        }
    }

    [NotMapped]
    public bool HasStatus => StatusLabel.Length > 0;

    [NotMapped]
    public bool IsAnnulation => StatusLabel == "Annulation";

    [NotMapped]
    public bool IsModification => StatusLabel == "Modification";

    [NotMapped]
    public string NoteBody
    {
        get
        {
            var title = DocumentTitle;
            if (title.StartsWith("Annulation ", StringComparison.Ordinal))
                return title["Annulation ".Length..].Trim();
            if (title.StartsWith("Modification ", StringComparison.Ordinal))
                return title["Modification ".Length..].Trim();
            if (title.StartsWith("Modif ", StringComparison.Ordinal))
                return title["Modif ".Length..].Trim();
            return title;
        }
    }

    [NotMapped]
    public string ParsedCommandeNumero
    {
        get
        {
            var match = CommandeNumeroRegex.Match(NoteBody);
            return match.Success ? match.Value : string.Empty;
        }
    }

    [NotMapped]
    public string ParsedPrimaryLabel
    {
        get
        {
            var parts = NoteBody
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => !CommandeNumeroRegex.IsMatch(p))
                .ToArray();
            return string.Join(" ", parts).Trim();
        }
    }

    // ---- Chips: labels from Note, navigation from joins ----

    [NotMapped]
    public string CommandeFallbackLabel { get; set; } = string.Empty;

    [NotMapped]
    public string CommandeChipLabel =>
        !string.IsNullOrEmpty(ParsedCommandeNumero) ? ParsedCommandeNumero : CommandeFallbackLabel;

    [NotMapped]
    public bool HasCommandeChip => !string.IsNullOrEmpty(CommandeChipLabel);

    [NotMapped]
    public bool IsOpenablePrimaryType =>
        OrigineType is "BL" or "BR" or "Avoir" or "AvoirFournisseur";

    [NotMapped]
    public string PrimaryChipLabel => IsOpenablePrimaryType ? ParsedPrimaryLabel : string.Empty;

    [NotMapped]
    public bool HasPrimaryChip => !string.IsNullOrEmpty(PrimaryChipLabel);

    [NotMapped]
    public bool PrimaryChipIsBonReception => OrigineType == "BR";

    // Navigation targets resolved via live joins (null => document/command removed).
    [NotMapped]
    public int? LinkedCommandeProductionId { get; set; }

    [NotMapped]
    public int? LinkedOperationProductionId { get; set; }

    [NotMapped]
    public bool PrimaryDocumentExists { get; set; }
}
