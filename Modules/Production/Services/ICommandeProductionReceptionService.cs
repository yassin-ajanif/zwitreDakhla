using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Production.Services;

public interface ICommandeProductionReceptionService
{
    /// <summary>Creates or updates the bon de réception linked to this commande production.</summary>
    Task SyncBonReceptionAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default);
}
