using GestionCommerciale.Modules.FactureFournisseur.Models;
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
        var br = await db.BonsReception
            .FirstAsync(b => b.Id == commande.BonReceptionId, cancellationToken);

        br.FournisseurId = commande.FournisseurId;
        br.Date = commande.DateCommande;
        br.Note = commande.Note;
        await db.SaveChangesAsync(cancellationToken);

        await SyncNaissainQtyPriceAsync(
            db,
            commande.BonReceptionId,
            commande.QuantiteNaissain,
            commande.PrixAchatNaissainHT,
            userId,
            cancellationToken);
    }

    public async Task SyncCommandeProductionAsync(
        AppDbContext db,
        BonReception bonReception,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var produitId = await _productionStock.EnsureNaissainProductAsync(db, cancellationToken);
        var naissainLine = bonReception.Lignes.FirstOrDefault(l => l.ProduitId == produitId);
        if (naissainLine is null || naissainLine.QuantiteRecue <= 0)
            return;

        await SyncNaissainQtyPriceAsync(
            db,
            bonReception.Id,
            naissainLine.QuantiteRecue,
            naissainLine.PrixUnitaireHT,
            userId,
            cancellationToken);
    }

    public async Task SyncNaissainQtyPriceAsync(
        AppDbContext db,
        int bonReceptionId,
        decimal quantite,
        decimal prixUnitaireHt,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (quantite <= 0)
            return;

        var produitId = await _productionStock.EnsureNaissainProductAsync(db, cancellationToken);
        var produit = await db.Produits.AsNoTracking().FirstAsync(p => p.Id == produitId, cancellationToken);

        var br = await db.BonsReception
            .Include(b => b.Lignes)
            .FirstAsync(b => b.Id == bonReceptionId, cancellationToken);

        var naissainLine = br.Lignes.FirstOrDefault(l => l.ProduitId == produitId);
        if (naissainLine is null)
        {
            naissainLine = new BonReceptionLigne
            {
                ProduitId = produitId,
                Designation = produit.Designation,
                QuantiteRecue = quantite,
                PrixUnitaireHT = prixUnitaireHt,
                TauxTVA = produit.TauxTVA
            };
            br.Lignes.Add(naissainLine);
        }
        else
        {
            naissainLine.Designation = produit.Designation;
            naissainLine.QuantiteRecue = quantite;
            naissainLine.PrixUnitaireHT = prixUnitaireHt;
            naissainLine.TauxTVA = produit.TauxTVA;
        }

        DocumentTotalsHelper.SyncBonReceptionTotalTtc(br);

        var commande = await db.CommandesProduction
            .FirstOrDefaultAsync(c => c.BonReceptionId == bonReceptionId, cancellationToken);
        if (commande is not null)
        {
            commande.QuantiteNaissain = (int)Math.Round(quantite, MidpointRounding.AwayFromZero);
            commande.PrixAchatNaissainHT = prixUnitaireHt;
        }

        if (br.FactureFournisseurId is { } factureId)
        {
            var invoiceLines = await db.FactureFournisseurLignes
                .Where(l => l.FactureFournisseurId == factureId
                            && l.BonReceptionId == bonReceptionId
                            && l.ProduitId == produitId)
                .ToListAsync(cancellationToken);

            if (invoiceLines.Count == 0)
            {
                db.FactureFournisseurLignes.Add(new FactureFournisseurLigne
                {
                    FactureFournisseurId = factureId,
                    BonReceptionId = bonReceptionId,
                    ProduitId = produitId,
                    Designation = produit.Designation,
                    Quantite = quantite,
                    PrixUnitaireHT = prixUnitaireHt,
                    TauxTVA = produit.TauxTVA
                });
            }
            else
            {
                var primary = invoiceLines[0];
                primary.Designation = produit.Designation;
                primary.Quantite = quantite;
                primary.PrixUnitaireHT = prixUnitaireHt;
                primary.TauxTVA = produit.TauxTVA;

                foreach (var duplicate in invoiceLines.Skip(1))
                    db.FactureFournisseurLignes.Remove(duplicate);
            }

            var facture = await db.FacturesFournisseurs
                .Include(f => f.Lignes)
                .FirstAsync(f => f.Id == factureId, cancellationToken);
            DocumentTotalsHelper.SyncFactureFournisseurTotalTtc(facture);
        }

        await db.SaveChangesAsync(cancellationToken);
        await _brWorkflow.ValiderAsync(br.Id, userId, cancellationToken);
    }
}
