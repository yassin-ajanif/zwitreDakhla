namespace GestionCommerciale.Modules.Production.ViewModels;

public sealed class ProductionListFilterOption
{
    public int? Id { get; init; }
    public string Label { get; init; } = string.Empty;

    public static ProductionListFilterOption All(string label) => new() { Id = null, Label = label };

    public static ProductionListFilterOption From(int id, string label) => new() { Id = id, Label = label };

    public override string ToString() => Label;
}
