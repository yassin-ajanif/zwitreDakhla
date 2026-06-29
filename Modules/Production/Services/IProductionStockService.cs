using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Production.Services;

public interface IProductionStockService
{
    Task<int> EnsureHuitreGrandProductAsync(AppDbContext db, CancellationToken cancellationToken = default);

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
}
