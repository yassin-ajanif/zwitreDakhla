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
            .FirstOrDefaultAsync(b => b.CommandeProductionId == commande.Id, cancellationToken);

        if (br is { FactureFournisseurId: not null })
            return;

        if (br == null)
        {
            br = new BonReception
            {
                Numero = await _numbers.NextBRAsync(cancellationToken),
                CommandeProductionId = commande.Id,
                FournisseurId = commande.FournisseurId,
                Date = commande.DateCommande,
                Note = commande.Note,
                CreatedByUserId = userId
            };
            db.BonsReception.Add(br);
        }
        else
        {
            br.FournisseurId = commande.FournisseurId;
            br.Date = commande.DateCommande;
            br.Note = commande.Note;
            db.BonReceptionLignes.RemoveRange(br.Lignes);
            br.Lignes.Clear();
        }

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
}
