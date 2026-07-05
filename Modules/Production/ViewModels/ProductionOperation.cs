using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using GestionCommerciale.Modules.Production.Models;

namespace GestionCommerciale.Modules.Production.ViewModels;

public partial class ProductionOperation : ObservableObject
{
    public const int MultiplierGrand = 160;
    public const int MultiplierMoyenne = 160;
    public const int MultiplierPetit = 160;
    public const int PochettesPerTable = 8;

    public static int ComputeGrandHuitres(int pochetteGrand) => pochetteGrand * MultiplierGrand;

    public static decimal ComputeTablesFromPochettes(int pochette) => pochette / (decimal)PochettesPerTable;

    public static string FormatTablesLabel(decimal tables) =>
        tables.ToString("N2", CultureInfo.CurrentCulture);

    public static int ComputeTotalHuitres(int pochetteGrand, int pochetteMoyenne, int pochettePetit) =>
        pochetteGrand * MultiplierGrand
        + pochetteMoyenne * MultiplierMoyenne
        + pochettePetit * MultiplierPetit;

    public static bool CanSaveOperation(
        int tables,
        int pochetteGrand,
        int pochetteMoyenne,
        int pochettePetit,
        int maxRemainingHuitresAtWater) =>
        tables > 0
        && ComputeGrandHuitres(pochetteGrand) <= maxRemainingHuitresAtWater;

    public static string FormatTauxMortaliteLabel(decimal percent) =>
        $"{percent.ToString("N0", CultureInfo.CurrentCulture)}%";

    public static int? ComputeDureeAgrandissementJours(DateTime dateCommande, DateTime? dateExpiration)
    {
        if (dateExpiration is not { } expiration)
            return null;

        var days = (expiration.Date - dateCommande.Date).Days;
        return Math.Max(0, days);
    }

    public static string FormatTauxAgrandissementLabel(int? jours) =>
        jours is int d
            ? string.Format(CultureInfo.CurrentCulture, "{0:N0} j", d)
            : "—";

    public static int ComputeRemainingHuitresAtWater(int quantiteNaissain, int sumGrandHuitres) =>
        Math.Max(0, quantiteNaissain - sumGrandHuitres);

    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool WasModified { get; set; }
    public string ModifiedAtLabel { get; set; } = string.Empty;
    public int Tables { get; set; }
    public int PochetteGrand { get; set; }
    public int PochetteMoyenne { get; set; }
    public int PochettePetit { get; set; }

    [ObservableProperty]
    private bool _isHighlighted;

    public int TotalGrand => PochetteGrand * MultiplierGrand;
    public int TotalMoyenne => PochetteMoyenne * MultiplierMoyenne;
    public int TotalPetit => PochettePetit * MultiplierPetit;
    public int TotalOperation => TotalGrand + TotalMoyenne + TotalPetit;

    public string DateShort => CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
    public string OperationTitle => DateShort;
    public string TablesLabel => Tables.ToString("N0", CultureInfo.CurrentCulture);
    public string PochetteGrandLabel => PochetteGrand.ToString("N0", CultureInfo.CurrentCulture);
    public string PochetteMoyenneLabel => PochetteMoyenne.ToString("N0", CultureInfo.CurrentCulture);
    public string PochettePetitLabel => PochettePetit.ToString("N0", CultureInfo.CurrentCulture);
    public string TablesGrandLabel => FormatTablesLabel(ComputeTablesFromPochettes(PochetteGrand));
    public string TablesMoyenneLabel => FormatTablesLabel(ComputeTablesFromPochettes(PochetteMoyenne));
    public string TablesPetitLabel => FormatTablesLabel(ComputeTablesFromPochettes(PochettePetit));
    public string TotalGrandLabel => TotalGrand.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalMoyenneLabel => TotalMoyenne.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalPetitLabel => TotalPetit.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalOperationLabel => TotalOperation.ToString("N0", CultureInfo.CurrentCulture);

    /// <summary>Mortality rate (%) = (quantité naissain − sum of grand huîtres) / quantité naissain × 100.</summary>
    public static decimal ComputeTauxMortalitePercent(int quantiteNaissain, int sumGrandHuitres)
    {
        if (quantiteNaissain <= 0) return 0;
        var mortalite = quantiteNaissain - sumGrandHuitres;
        return Math.Max(0, mortalite / (decimal)quantiteNaissain * 100m);
    }

    public static int SumGrandHuitres(IEnumerable<ProductionOperation> operations) =>
        operations.Sum(o => o.TotalGrand);

    public static int SumGrandHuitres(IEnumerable<OperationProduction> operations) =>
        operations.Sum(o => o.PochetteGrand * MultiplierGrand);

    public static ProductionOperation FromEntity(OperationProduction entity)
    {
        var wasModified = entity.UpdatedAt > entity.CreatedAt.AddSeconds(2);
        return new()
        {
            Id = entity.Id,
            Date = entity.OperationAt.Date,
            CreatedAt = entity.OperationAt,
            UpdatedAt = entity.UpdatedAt,
            WasModified = wasModified,
            ModifiedAtLabel = string.Empty,
            Tables = entity.Tables,
            PochetteGrand = entity.PochetteGrand,
            PochetteMoyenne = entity.PochetteMoyenne,
            PochettePetit = entity.PochettePetit
        };
    }

    public void ApplyTo(OperationProduction entity, DateTime? operationAt = null)
    {
        if (operationAt.HasValue)
            entity.OperationAt = operationAt.Value;

        entity.Tables = Tables;
        entity.PochetteGrand = PochetteGrand;
        entity.PochetteMoyenne = PochetteMoyenne;
        entity.PochettePetit = PochettePetit;
    }
}
