using GestionCommerciale.Modules.Production.ViewModels;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Production.Services;

public sealed class ProductionStockService : IProductionStockService
{
    public const string ZwittreGrandReference = "ZWITTRE-GRAND";
    public const string ZwittreGrandDesignation = "Zwittre Grand";

    private readonly IStockMovementService _stock;
    private readonly ILocaleService _locale;

    public ProductionStockService(IStockMovementService stock, ILocaleService locale)
    {
        _stock = stock;
        _locale = locale;
    }

    public async Task<int> EnsureZwittreGrandProductAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.Produits
            .FirstOrDefaultAsync(p => p.Reference == ZwittreGrandReference, cancellationToken);
        if (existing != null)
            return existing.Id;

        var product = new Produit
        {
            Reference = ZwittreGrandReference,
            Designation = ZwittreGrandDesignation,
            Unite = "U",
            Actif = true
        };
        db.Produits.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task SyncOperationStockAsync(
        AppDbContext db,
        int operationId,
        int pochetteGrand,
        DateTime operationAt,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var produitId = await EnsureZwittreGrandProductAsync(db, cancellationToken);
        var totalZwitres = pochetteGrand * ProductionOperation.MultiplierGrand;
        var note = BuildNote(operationAt);

        await _stock.SyncProductionStockAsync(
            db,
            operationId,
            produitId,
            totalZwitres,
            note,
            createdByUserId,
            cancellationToken);
    }

    public async Task RemoveOperationStockAsync(
        AppDbContext db,
        int operationId,
        DateTime operationAt,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var produitId = await EnsureZwittreGrandProductAsync(db, cancellationToken);
        var note = BuildNote(operationAt);

        await _stock.SyncProductionStockAsync(
            db,
            operationId,
            produitId,
            0,
            note,
            createdByUserId,
            cancellationToken);
    }

    private string BuildNote(DateTime operationAt) =>
        _locale.Tf("Prod_StockNoteFmt", operationAt.ToString("dd/MM/yyyy HH:mm"));
}
