using GestionCommerciale.Modules.Production.Models;
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

    /// <summary>Updates the bon de réception linked to this commande production.</summary>
    Task SyncBonReceptionAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default);
}
