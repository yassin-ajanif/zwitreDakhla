using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Production.Services;

public interface ICommandeProductionReceptionService
{
    /// <summary>Creates the bon de réception stub required before the first commande save.</summary>
    Task<int> EnsureBonReceptionIdAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default);

    /// <summary>Pushes commande header + naissain qty/price to BR and linked supplier invoice.</summary>
    Task SyncBonReceptionAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default);

    /// <summary>Pushes BR naissain qty/price to commande and linked supplier invoice.</summary>
    Task SyncCommandeProductionAsync(
        AppDbContext db,
        BonReception bonReception,
        int? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps commande, BR naissain line, and supplier-invoice naissain line in sync
    /// for the given bon de réception.
    /// </summary>
    Task SyncNaissainQtyPriceAsync(
        AppDbContext db,
        int bonReceptionId,
        decimal quantite,
        decimal prixUnitaireHt,
        int? userId,
        CancellationToken cancellationToken = default);
}
