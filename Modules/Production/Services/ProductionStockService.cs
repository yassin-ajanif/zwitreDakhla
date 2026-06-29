using GestionCommerciale.Modules.Production.ViewModels;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Production.Services;

public sealed class ProductionStockService : IProductionStockService
{
    public const string LegacyHuitreGrandReference = "ZWITTRE-GRAND";

    public const string HuitreGrandReference = "HUITRE-GRAND";
    public const string HuitreGrandDesignation = "Huître Grand";

    private readonly IStockMovementService _stock;
    private readonly ILocaleService _locale;

    public ProductionStockService(IStockMovementService stock, ILocaleService locale)
    {
        _stock = stock;
        _locale = locale;
    }

    public async Task<int> EnsureHuitreGrandProductAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.Produits
            .FirstOrDefaultAsync(p =>
                p.Reference == HuitreGrandReference || p.Reference == LegacyHuitreGrandReference,
                cancellationToken);
        if (existing != null)
        {
            if (existing.Reference != HuitreGrandReference)
                existing.Reference = HuitreGrandReference;
            if (existing.Designation != HuitreGrandDesignation)
                existing.Designation = HuitreGrandDesignation;
            if (existing.Reference != HuitreGrandReference || existing.Designation != HuitreGrandDesignation)
                await db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var product = new Produit
        {
            Reference = HuitreGrandReference,
            Designation = HuitreGrandDesignation,
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
        var produitId = await EnsureHuitreGrandProductAsync(db, cancellationToken);
        var totalHuitres = pochetteGrand * ProductionOperation.MultiplierGrand;
        var note = BuildNote(operationAt);

        await _stock.SyncProductionStockAsync(
            db,
            operationId,
            produitId,
            totalHuitres,
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
        var produitId = await EnsureHuitreGrandProductAsync(db, cancellationToken);
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
