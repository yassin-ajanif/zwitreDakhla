using System.Globalization;
using GestionCommerciale.Modules.Production.Models;

namespace GestionCommerciale.Modules.Production.ViewModels;

public class ProductionOperation
{
    public const int MultiplierGrand = 160;
    public const int MultiplierMoyenne = 160;
    public const int MultiplierPetit = 160;

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
