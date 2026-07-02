using System.Globalization;
using GestionCommerciale.Modules.Production.Models;

namespace GestionCommerciale.Modules.Production.ViewModels;

public class ProductionOperation
{
    public const int MultiplierGrand = 160;
    public const int MultiplierMoyenne = 160;
    public const int MultiplierPetit = 160;
    public const int PochettesPerTable = 8;

    public static int ExpectedTotalHuitres(int tables) => tables * PochettesPerTable * MultiplierGrand;

    public static int ExpectedTotalPochets(int tables) => tables * PochettesPerTable;

    public static int ComputeTotalHuitres(int pochetteGrand, int pochetteMoyenne, int pochettePetit) =>
        pochetteGrand * MultiplierGrand
        + pochetteMoyenne * MultiplierMoyenne
        + pochettePetit * MultiplierPetit;

    public static bool TotalsMatchTables(int tables, int pochetteGrand, int pochetteMoyenne, int pochettePetit) =>
        ComputeTotalHuitres(pochetteGrand, pochetteMoyenne, pochettePetit) == ExpectedTotalHuitres(tables);

    public static string FormatTauxMortaliteLabel(decimal percent) =>
        $"{percent.ToString("N0", CultureInfo.CurrentCulture)}%";

    /// <summary>0 = grand, 1 = moyenne, 2 = petit. Hint when exactly one pochette field is still 0.</summary>
    public static bool TryGetRemainingForSingleEmptyPochette(
        int tables,
        int pochetteGrand,
        int pochetteMoyenne,
        int pochettePetit,
        out int emptyFieldIndex,
        out int remainingPochets)
    {
        emptyFieldIndex = -1;
        remainingPochets = 0;
        if (tables <= 0) return false;

        var emptyCount = 0;
        if (pochetteGrand == 0)
        {
            emptyFieldIndex = 0;
            emptyCount++;
        }

        if (pochetteMoyenne == 0)
        {
            emptyFieldIndex = 1;
            emptyCount++;
        }

        if (pochettePetit == 0)
        {
            emptyFieldIndex = 2;
            emptyCount++;
        }

        if (emptyCount != 1) return false;

        remainingPochets = ExpectedTotalPochets(tables) - pochetteGrand - pochetteMoyenne - pochettePetit;
        return remainingPochets >= 0;
    }

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
