using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Production.Models;

public class TypeNaissain : BaseEntity
{
    public string Nom { get; set; } = string.Empty;
    public bool Actif { get; set; } = true;
    public int Ordre { get; set; }
}
