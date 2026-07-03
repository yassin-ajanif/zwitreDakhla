using GestionCommerciale.Shared.Models;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;

namespace GestionCommerciale.Modules.Production.Models;

public class CommandeProduction : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int FournisseurId { get; set; }
    public TiersEntity? Fournisseur { get; set; }
    public int TypeNaissainId { get; set; }
    public TypeNaissain? TypeNaissain { get; set; }
    public int CategorieCommandeId { get; set; }
    public CategorieCommande? CategorieCommande { get; set; }
    public int QuantiteNaissain { get; set; }
    public decimal PrixAchatNaissainHT { get; set; }
    public decimal TauxMortalite { get; set; }
    public DateTime DateCommande { get; set; }
    public DateTime? DateExpiration { get; set; }
    public bool EstTerminee { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<OperationProduction> Operations { get; set; } = [];
}
