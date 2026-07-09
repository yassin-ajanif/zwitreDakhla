using GestionCommerciale.Modules.Production.Models;
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

    public const string NaissainReference = "NAISSAIN";
    public const string NaissainDesignation = "Naissain";

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

    public async Task<int> EnsureNaissainProductAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.Produits
            .FirstOrDefaultAsync(p => p.Reference == NaissainReference, cancellationToken);
        if (existing != null)
        {
            if (existing.Designation != NaissainDesignation)
            {
                existing.Designation = NaissainDesignation;
                await db.SaveChangesAsync(cancellationToken);
            }
            return existing.Id;
        }

        var product = new Produit
        {
            Reference = NaissainReference,
            Designation = NaissainDesignation,
            Unite = "U",
            TauxTVA = 20,
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
        var note = await BuildNoteAsync(db, operationId, operationAt, cancellationToken);

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
        var note = await BuildNoteAsync(db, operationId, operationAt, cancellationToken);

        await _stock.SyncProductionStockAsync(
            db,
            operationId,
            produitId,
            0,
            note,
            createdByUserId,
            cancellationToken);
    }

    public async Task RemoveCommandeStockAsync(
        AppDbContext db,
        int commandeId,
        IReadOnlyList<OperationProduction> operations,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        foreach (var operation in operations)
        {
            await RemoveOperationStockAsync(
                db,
                operation.Id,
                operation.OperationAt,
                createdByUserId,
                cancellationToken);
        }

        var br = await db.BonsReception
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.CommandeProductionId == commandeId, cancellationToken);

        if (br is null || br.FactureFournisseurId is not null)
            return;

        var commandeNumero = await db.CommandesProduction.AsNoTracking()
            .Where(c => c.Id == commandeId)
            .Select(c => c.Numero)
            .FirstOrDefaultAsync(cancellationToken);
        var noteDetail = string.IsNullOrWhiteSpace(commandeNumero)
            ? br.Numero
            : $"{commandeNumero.Trim()} | {br.Numero}";

        await _stock.SyncBonReceptionStockAsync(
            db,
            br.Id,
            noteDetail,
            [],
            createdByUserId,
            cancellationToken);

        db.BonsReception.Remove(br);
    }

    private async Task<string> BuildNoteAsync(
        AppDbContext db,
        int operationId,
        DateTime operationAt,
        CancellationToken cancellationToken)
    {
        var commandeNumero = await db.OperationsProduction.AsNoTracking()
            .Where(o => o.Id == operationId && o.CommandeProductionId != null)
            .Select(o => o.CommandeProduction!.Numero)
            .FirstOrDefaultAsync(cancellationToken);

        var dateSuffix = operationAt.ToString("dd/MM/yyyy HH:mm");
        return string.IsNullOrWhiteSpace(commandeNumero)
            ? _locale.Tf("Prod_StockNoteFmt", dateSuffix)
            : $"{commandeNumero.Trim()} {dateSuffix}";
    }
}
