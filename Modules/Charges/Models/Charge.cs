using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Charges.Models;

public class Charge : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int CategorieChargeId { get; set; }
    public CategorieCharge? CategorieCharge { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public int? FournisseurId { get; set; }
    public string Fournisseur { get; set; } = string.Empty;
    public decimal MontantTtc { get; set; }
    public bool EstPayee { get; set; }
    public string Note { get; set; } = string.Empty;
}
