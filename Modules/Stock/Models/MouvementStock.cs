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
    public string TraceDetail => DocumentRef;

    [NotMapped]
    public string UnitPriceDetail { get; set; } = string.Empty;

    [NotMapped]
    public bool HasUnitPriceDetail => !string.IsNullOrEmpty(UnitPriceDetail);

    [NotMapped]
    public int? LinkedCommandeProductionId { get; set; }

    [NotMapped]
    public string LinkedCommandeProductionLabel { get; set; } = string.Empty;

    [NotMapped]
    public bool HasLinkedCommandeProduction => LinkedCommandeProductionId is > 0;
}
