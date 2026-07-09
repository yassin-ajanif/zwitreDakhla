using System.Diagnostics;
using System.Globalization;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Shared.Services;

public sealed class ProductionPerformanceTestService
{
    private const int CommandCount = 500;
    private const int OperationsPerCommand = 365;
    private const int NaissainQty = 1_000_000;
    private const decimal NaissainPrice = 1.5m;

    private readonly string _cs;

    public ProductionPerformanceTestService()
    {
        _cs = DatabasePath.GetConnectionString();
    }

    public async Task<string> RunAsync(IProgress<string> progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA synchronous=OFF; PRAGMA journal_mode=WAL; PRAGMA cache_size=-500000; PRAGMA temp_store=MEMORY;";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var max = await GetMaxIdsAsync(conn, ct);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var startDate = DateTime.Today.AddDays(-(CommandCount - 1));

        progress.Report("Vérification des données de référence (fournisseur, type, catégorie)...");
        var refs = await EnsurePrerequisitesAsync(conn, max, now, ct);

        progress.Report($"Création de {CommandCount:N0} bons de réception...");
        await InsertBonReceptionsAsync(conn, max.BrId, refs.FournisseurId, now, startDate, ct);

        progress.Report($"Création de {CommandCount:N0} commandes de production...");
        await InsertCommandesAsync(conn, max, refs, now, startDate, ct);

        progress.Report($"Création de {CommandCount * OperationsPerCommand:N0} opérations ({OperationsPerCommand}/commande)...");
        var operationCount = await InsertOperationsAsync(conn, max, startDate, now, ct);

        progress.Report("Mesure du chargement liste (comme l'écran Production)...");
        var listMs = await MeasureListLoadAsync(ct);

        progress.Report("Mesure du chargement d'une commande avec toutes ses opérations...");
        var editMs = await MeasureCommandLoadAsync(ct);

        sw.Stop();
        var e = sw.Elapsed;
        return
            $"Terminé en {e.Hours}h {e.Minutes}m {e.Seconds}s ({e.TotalSeconds:F1}s) — " +
            $"{CommandCount:N0} commandes, {operationCount:N0} opérations. " +
            $"Chargement liste: {listMs:F0} ms, chargement 1 commande ({OperationsPerCommand} ops): {editMs:F0} ms.";
    }

    private static async Task<(long BrId, long CmdId, long OpId, long TiersId, long TypeId, long CategorieId)> GetMaxIdsAsync(
        SqliteConnection conn, CancellationToken ct)
    {
        async Task<long> Max(string table)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT IFNULL(MAX(Id),0) FROM \"{table}\"";
            var r = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(r);
        }

        return (
            await Max("BonsReception"),
            await Max("CommandesProduction"),
            await Max("OperationsProduction"),
            await Max("Tiers"),
            await Max("TypesHuitre"),
            await Max("CategoriesCommande")
        );
    }

    private sealed record ProductionRefs(int FournisseurId, int TypeHuitreId, int CategorieCommandeId);

    private static async Task<ProductionRefs> EnsurePrerequisitesAsync(
        SqliteConnection conn,
        (long BrId, long CmdId, long OpId, long TiersId, long TypeId, long CategorieId) max,
        string now,
        CancellationToken ct)
    {
        var fournisseurId = await ScalarIntAsync(conn,
            "SELECT Id FROM Tiers WHERE Actif=1 AND (Type=1 OR Type=2) ORDER BY Id LIMIT 1", ct);
        if (fournisseurId == 0)
        {
            fournisseurId = (int)(max.TiersId + 1);
            await ExecAsync(conn,
                $@"INSERT INTO Tiers (Id,CreatedAt,UpdatedAt,Type,Nom,ICE,Adresse,Ville,Telephone,Email,ConditionsPaiement,Actif)
                   VALUES ({fournisseurId},'{now}','{now}',{(int)TypeTiers.Fournisseur},'Fournisseur perf test','','','','','','',1)",
                ct);
        }

        var typeId = await ScalarIntAsync(conn,
            "SELECT Id FROM TypesHuitre WHERE Actif=1 ORDER BY Ordre, Id LIMIT 1", ct);
        if (typeId == 0)
        {
            typeId = (int)(max.TypeId + 1);
            await ExecAsync(conn,
                $@"INSERT INTO TypesHuitre (Id,CreatedAt,UpdatedAt,Nom,Actif,Ordre,CreatedByUserId)
                   VALUES ({typeId},'{now}','{now}','Type perf test',1,0,NULL)",
                ct);
        }

        var categorieId = await ScalarIntAsync(conn,
            "SELECT Id FROM CategoriesCommande WHERE Actif=1 ORDER BY Ordre, Id LIMIT 1", ct);
        if (categorieId == 0)
        {
            categorieId = (int)(max.CategorieId + 1);
            await ExecAsync(conn,
                $@"INSERT INTO CategoriesCommande (Id,CreatedAt,UpdatedAt,Nom,Actif,Ordre,CreatedByUserId)
                   VALUES ({categorieId},'{now}','{now}','Catégorie perf test',1,0,NULL)",
                ct);
        }

        return new ProductionRefs(fournisseurId, typeId, categorieId);
    }

    private static async Task InsertBonReceptionsAsync(
        SqliteConnection conn,
        long startBrId,
        int fournisseurId,
        string now,
        DateTime startDate,
        CancellationToken ct)
    {
        const int batch = 100;
        var startBr = startBrId + 1;

        for (var i = 0; i < CommandCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO BonsReception (Id,CreatedAt,UpdatedAt,Numero,FournisseurId,BonCommandeId,FactureFournisseurId,Date,Note,TotalTtc,CreatedByUserId) VALUES ");
            var end = Math.Min(i + batch, CommandCount);
            for (var j = i; j < end; j++)
            {
                var id = startBr + j;
                var date = startDate.AddDays(j).ToString("yyyy-MM-dd");
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture,
                    $"({id},'{now}','{now}','BR-PERF-{j:D6}',{fournisseurId},NULL,NULL,'{date}','Perf test',0,NULL)");
            }

            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task InsertCommandesAsync(
        SqliteConnection conn,
        (long BrId, long CmdId, long OpId, long TiersId, long TypeId, long CategorieId) max,
        ProductionRefs refs,
        string now,
        DateTime startDate,
        CancellationToken ct)
    {
        const int batch = 100;
        var startCmd = max.CmdId + 1;
        var startBr = max.BrId + 1;

        for (var i = 0; i < CommandCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(@"INSERT INTO CommandesProduction
                (Id,CreatedAt,UpdatedAt,Numero,BonReceptionId,FournisseurId,TypeHuitreId,CategorieCommandeId,
                 QuantiteNaissain,PrixAchatNaissainHT,TauxMortalite,DateCommande,DateExpiration,EstTerminee,Note,CreatedByUserId) VALUES ");
            var end = Math.Min(i + batch, CommandCount);
            for (var j = i; j < end; j++)
            {
                var id = startCmd + j;
                var brId = startBr + j;
                var date = startDate.AddDays(j).ToString("yyyy-MM-dd");
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture,
                    $"({id},'{now}','{now}','CMD-PERF-{j:D6}',{brId},{refs.FournisseurId},{refs.TypeHuitreId},{refs.CategorieCommandeId}," +
                    $"{NaissainQty},{NaissainPrice:F2},0,'{date}',NULL,0,'Perf test',NULL)");
            }

            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task<int> InsertOperationsAsync(
        SqliteConnection conn,
        (long BrId, long CmdId, long OpId, long TiersId, long TypeId, long CategorieId) max,
        DateTime startDate,
        string now,
        CancellationToken ct)
    {
        const int batch = 2000;
        var startCmd = max.CmdId + 1;
        var startOp = max.OpId + 1;
        var opIndex = 0;
        System.Text.StringBuilder? sb = null;
        var total = CommandCount * OperationsPerCommand;

        for (var cmdIdx = 0; cmdIdx < CommandCount; cmdIdx++)
        {
            var cmdId = startCmd + cmdIdx;
            var cmdDate = startDate.AddDays(cmdIdx);

            for (var day = 0; day < OperationsPerCommand; day++)
            {
                if (opIndex % batch == 0)
                {
                    if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
                    sb = new System.Text.StringBuilder();
                    sb.Append(@"INSERT INTO OperationsProduction
                        (Id,CreatedAt,UpdatedAt,CommandeProductionId,OperationAt,Tables,PochetteGrand,PochetteMoyenne,PochettePetit,CreatedByUserId) VALUES ");
                }
                else
                {
                    sb!.Append(',');
                }

                var id = startOp + opIndex;
                var opAt = cmdDate.AddDays(day).AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
                sb!.Append(CultureInfo.InvariantCulture,
                    $"({id},'{now}','{now}',{cmdId},'{opAt}',1,1,0,0,NULL)");
                opIndex++;
            }
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
        return total;
    }

    private async Task<double> MeasureListLoadAsync(CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_cs)
            .Options;

        await using var db = new AppDbContext(options);
        var sw = Stopwatch.StartNew();
        _ = await db.CommandesProduction.AsNoTracking()
            .Include(c => c.Fournisseur)
            .Include(c => c.CategorieCommande)
            .Include(c => c.TypeHuitre)
            .Include(c => c.Operations)
            .OrderByDescending(c => c.DateCommande)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private async Task<double> MeasureCommandLoadAsync(CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_cs)
            .Options;

        await using var db = new AppDbContext(options);
        var cmdId = await db.CommandesProduction.AsNoTracking()
            .Where(c => c.Numero.StartsWith("CMD-PERF-"))
            .OrderByDescending(c => c.Id)
            .Select(c => c.Id)
            .FirstAsync(ct);

        var sw = Stopwatch.StartNew();
        _ = await db.CommandesProduction.AsNoTracking()
            .Include(c => c.Operations)
            .FirstAsync(c => c.Id == cmdId, ct);
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static async Task<int> ScalarIntAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var r = await cmd.ExecuteScalarAsync(ct);
        return r is null or DBNull ? 0 : Convert.ToInt32(r);
    }

    private static async Task ExecAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var tx = conn.BeginTransaction();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        await cmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }
}
