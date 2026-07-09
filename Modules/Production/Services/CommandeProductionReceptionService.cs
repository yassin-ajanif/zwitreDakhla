using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Modules.Reception.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Production.Services;

public sealed class CommandeProductionReceptionService : ICommandeProductionReceptionService
{
    private readonly IDocumentNumberService _numbers;
    private readonly IProductionStockService _productionStock;
    private readonly IBonReceptionWorkflowService _brWorkflow;

    public CommandeProductionReceptionService(
        IDocumentNumberService numbers,
        IProductionStockService productionStock,
        IBonReceptionWorkflowService brWorkflow)
    {
        _numbers = numbers;
        _productionStock = productionStock;
        _brWorkflow = brWorkflow;
    }

    public async Task<int> EnsureBonReceptionIdAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (commande.BonReceptionId > 0)
            return commande.BonReceptionId;

        var br = new BonReception
        {
            Numero = await _numbers.NextBRAsync(cancellationToken),
            FournisseurId = commande.FournisseurId,
            Date = commande.DateCommande,
            Note = commande.Note,
            CreatedByUserId = userId
        };
        db.BonsReception.Add(br);
        await db.SaveChangesAsync(cancellationToken);
        commande.BonReceptionId = br.Id;
        return br.Id;
    }

    public async Task SyncBonReceptionAsync(
        AppDbContext db,
        CommandeProduction commande,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var produitId = await _productionStock.EnsureNaissainProductAsync(db, cancellationToken);
        var produit = await db.Produits.AsNoTracking().FirstAsync(p => p.Id == produitId, cancellationToken);

        var br = await db.BonsReception
            .Include(b => b.Lignes)
            .FirstAsync(b => b.Id == commande.BonReceptionId, cancellationToken);

        if (br.FactureFournisseurId is not null)
            return;

        br.FournisseurId = commande.FournisseurId;
        br.Date = commande.DateCommande;
        br.Note = commande.Note;
        db.BonReceptionLignes.RemoveRange(br.Lignes);
        br.Lignes.Clear();

        br.Lignes.Add(new BonReceptionLigne
        {
            ProduitId = produitId,
            Designation = produit.Designation,
            QuantiteRecue = commande.QuantiteNaissain,
            PrixUnitaireHT = commande.PrixAchatNaissainHT,
            TauxTVA = produit.TauxTVA
        });

        DocumentTotalsHelper.SyncBonReceptionTotalTtc(br);
        await db.SaveChangesAsync(cancellationToken);
        await _brWorkflow.ValiderAsync(br.Id, userId, cancellationToken);
    }

    public async Task SyncCommandeProductionAsync(
        AppDbContext db,
        BonReception bonReception,
        CancellationToken cancellationToken = default)
    {
        var commande = await db.CommandesProduction
            .FirstOrDefaultAsync(c => c.BonReceptionId == bonReception.Id, cancellationToken);
        if (commande is null)
            return;

        var produitId = await _productionStock.EnsureNaissainProductAsync(db, cancellationToken);
        var naissainLine = bonReception.Lignes.FirstOrDefault(l => l.ProduitId == produitId);
        if (naissainLine is null || naissainLine.QuantiteRecue <= 0)
            return;

        commande.QuantiteNaissain = (int)naissainLine.QuantiteRecue;
        commande.PrixAchatNaissainHT = naissainLine.PrixUnitaireHT;
        await db.SaveChangesAsync(cancellationToken);
    }
}
