using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Production.Models;

public class OperationProduction : BaseEntity
{
    public int? CommandeProductionId { get; set; }
    public CommandeProduction? CommandeProduction { get; set; }

    public DateTime OperationAt { get; set; }
    public int Tables { get; set; }
    public int PochetteGrand { get; set; }
    public int PochetteMoyenne { get; set; }
    public int PochettePetit { get; set; }
}
