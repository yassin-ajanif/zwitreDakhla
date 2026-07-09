using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Production.Services;

public interface IProductionStockService
{
    Task<int> EnsureHuitreGrandProductAsync(AppDbContext db, CancellationToken cancellationToken = default);

    Task<int> EnsureNaissainProductAsync(AppDbContext db, CancellationToken cancellationToken = default);

    Task SyncOperationStockAsync(
        AppDbContext db,
        int operationId,
        int pochetteGrand,
        DateTime operationAt,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task RemoveOperationStockAsync(
        AppDbContext db,
        int operationId,
        DateTime operationAt,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Reverses stock for all operations and the linked auto-created BR (if not invoiced).</summary>
    Task RemoveCommandeStockAsync(
        AppDbContext db,
        int commandeId,
        IReadOnlyList<Models.OperationProduction> operations,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the linked BR after its commande has been removed.</summary>
    Task RemoveLinkedBonReceptionAsync(
        AppDbContext db,
        int bonReceptionId,
        CancellationToken cancellationToken = default);
}
