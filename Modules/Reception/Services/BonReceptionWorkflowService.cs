using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reception.Services;

public sealed class BonReceptionWorkflowService : IBonReceptionWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public BonReceptionWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task ValiderAsync(int bonReceptionId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var br = await db.BonsReception
            .Include(b => b.Lignes)
            .FirstAsync(b => b.Id == bonReceptionId, cancellationToken);

        await ReplayBonReceptionLinesIntoStockAsync(db, br, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    public async Task ResyncStockFromLinesAsync(int bonReceptionId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var br = await db.BonsReception
            .Include(b => b.Lignes)
            .FirstAsync(b => b.Id == bonReceptionId, cancellationToken);

        await ReplayBonReceptionLinesIntoStockAsync(db, br, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    private async Task ReplayBonReceptionLinesIntoStockAsync(
        AppDbContext db,
        BonReception br,
        int? userId,
        CancellationToken cancellationToken)
    {
        var noteDetail = br.Numero;
        if (br.CommandeProductionId is int commandeId)
        {
            var commandeNumero = await db.CommandesProduction.AsNoTracking()
                .Where(c => c.Id == commandeId)
                .Select(c => c.Numero)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(commandeNumero))
                noteDetail = $"{commandeNumero.Trim()} | {br.Numero}";
        }

        await _stock.SyncBonReceptionStockAsync(
            db,
            br.Id,
            noteDetail,
            br.Lignes
                .Where(l => l.QuantiteRecue > 0)
                .Select(l => (l.ProduitId, l.QuantiteRecue, l.PrixUnitaireHT)),
            userId,
            cancellationToken);
    }
}
