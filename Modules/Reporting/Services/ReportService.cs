using GestionCommerciale.Modules.AvoirFournisseur.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reporting.Services;

public sealed class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppSettingsService _settings;
    private readonly ILocaleService _locale;

    public ReportService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAppSettingsService settings,
        ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _settings = settings;
        _locale = locale;
    }

    public async Task<List<ReportSaleByProductRow>> GetSalesByProductAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var lignes = await db.FactureLignes.AsNoTracking()
            .Where(l => l.Facture!.Date >= from && l.Facture.Date < toEnd)
            .Select(l => new
            {
                l.ProduitId,
                l.Quantite,
                l.PrixUnitaireHT,
                l.Remise,
                l.TauxTVA,
                l.Designation
            })
            .ToListAsync(ct);

        var prodIds = lignes.Select(l => l.ProduitId).Distinct().ToList();
        var produits = await db.Produits.AsNoTracking()
            .Where(p => prodIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.PrixAchatHT, Categorie = p.Categorie != null ? p.Categorie.Nom : "" })
            .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var grouped = lignes
            .GroupBy(l => l.ProduitId)
            .Select(g =>
            {
                var p = prodMap.GetValueOrDefault(g.Key);
                var prixAchat = p?.PrixAchatHT ?? 0;
                var ht = g.Sum(l => DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise));
                var cost = g.Sum(l => l.Quantite * prixAchat);
                var profit = ht - cost;
                var tva = g.Sum(l => DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise) * (l.TauxTVA / 100m));
                var marginPct = ht > 0 ? profit / ht * 100m : 0;
                return new ReportSaleByProductRow(
                    p?.Reference ?? string.Empty,
                    p?.Designation ?? g.First().Designation,
                    p?.Categorie ?? string.Empty,
                    g.Sum(l => l.Quantite),
                    ht,
                    ht + tva,
                    dev,
                    profit,
                    marginPct);
            })
            .OrderByDescending(r => r.TotalTtc)
            .ToList();

        return grouped;
    }

    public async Task<List<ReportSaleByCustomerRow>> GetSalesByCustomerAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.Id,
                f.ClientId,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA,
                    l.Designation
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = factures.Select(f => f.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom, t.ICE, t.Ville })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        var allProdIds = factures.SelectMany(f => f.Lignes).Select(l => l.ProduitId).Distinct().ToList();
        var produits = await db.Produits.AsNoTracking()
            .Where(p => allProdIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.PrixAchatHT })
            .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var grouped = factures
            .GroupBy(f => f.ClientId)
            .Select(g =>
            {
                var c = clientMap.GetValueOrDefault(g.Key);

                var allLignes = g.SelectMany(f => f.Lignes).ToList();

                // Per-product sub-rows (profit before global discount)
                var products = allLignes
                    .GroupBy(l => l.ProduitId)
                    .Select(pg =>
                    {
                        var p = prodMap.GetValueOrDefault(pg.Key);
                        var prixAchat = p?.PrixAchatHT ?? 0;
                        var ht = pg.Sum(l => DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise));
                        var cost = pg.Sum(l => l.Quantite * prixAchat);
                        var profit = ht - cost;
                        var tva = pg.Sum(l => DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise) * (l.TauxTVA / 100m));
                        var marginPct = ht > 0 ? profit / ht * 100m : 0;
                        return new ReportSaleByCustomerProductRow(
                            p?.Reference ?? string.Empty,
                            p?.Designation ?? pg.First().Designation,
                            pg.Sum(l => l.Quantite),
                            ht,
                            ht + tva,
                            dev,
                            profit,
                            marginPct);
                    })
                    .OrderByDescending(pr => pr.TotalTtc)
                    .ToList();

                // Client-level totals with profit (global discount applied)
                decimal totalHt = 0, totalTva = 0, totalCost = 0;
                foreach (var f in g)
                {
                    var factor = 1 - f.RemiseGlobale / 100m;
                    foreach (var l in f.Lignes)
                    {
                        var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
                        var prixAchat = prodMap.GetValueOrDefault(l.ProduitId)?.PrixAchatHT ?? 0;
                        totalHt += lht * factor;
                        totalTva += lht * (l.TauxTVA / 100m) * factor;
                        totalCost += l.Quantite * prixAchat;
                    }
                }
                var totalProfit = totalHt - totalCost;
                var marginPct = totalHt > 0 ? totalProfit / totalHt * 100m : 0;

                return new ReportSaleByCustomerRow(
                    c?.Nom ?? string.Empty,
                    c?.ICE ?? string.Empty,
                    c?.Ville ?? string.Empty,
                    g.Count(),
                    totalHt,
                    totalHt + totalTva,
                    dev,
                    totalProfit,
                    marginPct,
                    products);
            })
            .OrderByDescending(r => r.TotalTtc)
            .ToList();

        return grouped;
    }

    public async Task<List<ReportRefundRow>> GetRefundsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var avoirs = await db.Avoirs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .OrderByDescending(a => a.Date)
            .Select(a => new
            {
                a.Id,
                a.Numero,
                a.Date,
                a.ClientId,
                a.Motif,
                a.RetourMarchandise,
                Lignes = a.Lignes!.Select(l => new
                {
                    l.Quantite, l.PrixUnitaireHT, l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = avoirs.Select(a => a.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        return avoirs.Select(a =>
        {
            var lignes = a.Lignes.Select(l => new AvoirLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                TauxTVA = l.TauxTVA
            }).ToList();
            return new ReportRefundRow(
                a.Numero ?? string.Empty,
                a.Date,
                clientMap.GetValueOrDefault(a.ClientId)?.Nom ?? string.Empty,
                a.Motif ?? string.Empty,
                a.RetourMarchandise,
                DocumentTotalsHelper.AvoirTotals(lignes).ttc,
                dev);
        }).ToList();
    }

    public async Task<List<ReportDailySaleRow>> GetDailySalesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .OrderBy(f => f.Date)
            .Select(f => new
            {
                f.Id,
                f.ClientId,
                f.Numero,
                f.Date,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = factures.Select(f => f.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        var allProdIds = factures.SelectMany(f => f.Lignes).Select(l => l.ProduitId).Distinct().ToList();
        var produits = await db.Produits.AsNoTracking()
            .Where(p => allProdIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PrixAchatHT })
            .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var grouped = factures
            .GroupBy(f => f.Date.Date)
            .Select(g =>
            {
                decimal dayHt = 0, dayTva = 0, dayCost = 0;

                var details = g.Select(f =>
                {
                    var factor = 1 - f.RemiseGlobale / 100m;
                    decimal ht = 0, tva = 0, cost = 0;
                    foreach (var l in f.Lignes)
                    {
                        var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
                        var prixAchat = prodMap.GetValueOrDefault(l.ProduitId)?.PrixAchatHT ?? 0;
                        ht += lht;
                        tva += lht * (l.TauxTVA / 100m);
                        cost += l.Quantite * prixAchat;
                    }
                    ht *= factor;
                    tva *= factor;
                    dayHt += ht;
                    dayTva += tva;
                    dayCost += cost;
                    var profit = ht - cost;
                    var marginPct = ht > 0 ? profit / ht * 100m : 0;
                    return new ReportDailySaleDetailRow(
                        f.Numero ?? string.Empty,
                        clientMap.GetValueOrDefault(f.ClientId)?.Nom ?? string.Empty,
                        ht,
                        ht + tva,
                        dev,
                        profit,
                        marginPct);
                }).ToList();

                var dayProfit = dayHt - dayCost;
                var dayMargin = dayHt > 0 ? dayProfit / dayHt * 100m : 0;

                return new ReportDailySaleRow(
                    g.Key,
                    g.Count(),
                    dayHt,
                    dayTva,
                    dayHt + dayTva,
                    dev,
                    dayProfit,
                    dayMargin,
                    details);
            })
            .OrderByDescending(r => r.Date)
            .ToList();

        return grouped;
    }

    public async Task<List<ReportUnpaidRow>> GetUnpaidSalesAsync(CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        var now = DateTime.Today;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var unpaid = await db.Factures.AsNoTracking()
            .Where(f => !f.EstPayee)
            .OrderBy(f => f.DateEcheance)
            .Take(200)
            .Select(f => new
            {
                f.Numero,
                f.DateEcheance,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.Quantite, l.PrixUnitaireHT, l.Remise, l.TauxTVA
                }).ToList(),
                Paiements = f.Paiements!.Select(p => p.Montant).ToList()
            })
            .ToListAsync(ct);

        var rows = new List<ReportUnpaidRow>();
        foreach (var f in unpaid)
        {
            var lignes = f.Lignes.Select(l => new FactureLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            }).ToList();
            var (_, _, ttc) = DocumentTotalsHelper.FactureTotals(lignes, f.RemiseGlobale);
            var paye = f.Paiements.Sum();
            var reste = ttc - paye;
            if (reste <= 0.01m) continue;

            var due = f.DateEcheance.Date;
            var daysFromDue = (now - due).Days;
            string dueStatus;
            var isOverdue = daysFromDue > 0;
            var isDueSoon = false;
            if (daysFromDue > 0)
                dueStatus = _locale.Tf("Report_UnpaidOverdueFmt", daysFromDue.ToString());
            else if (daysFromDue == 0)
                dueStatus = _locale.T("Report_UnpaidDueToday");
            else
            {
                var until = -daysFromDue;
                dueStatus = _locale.Tf("Report_UnpaidDueInFmt", until.ToString());
                if (until <= 7)
                    isDueSoon = true;
            }

            rows.Add(new ReportUnpaidRow(
                f.Numero ?? string.Empty,
                CurrencyHelper.Format(reste, dev),
                f.DateEcheance.ToString("d"),
                dueStatus,
                isOverdue,
                isDueSoon));
        }

        return rows;
    }

    public async Task<List<ReportStockMovementRow>> GetStockMovementsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var mouvements = await db.MouvementsStock.AsNoTracking()
            .Where(m => m.CreatedAt >= from && m.CreatedAt < toEnd)
            .Include(m => m.Produit)
            .OrderByDescending(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(500)
            .ToListAsync(ct);

        return mouvements.Select(m =>
        {
            var typeStr = m.Type switch
            {
                TypeMouvement.Entree => _locale.T("TypeMvt_Entree"),
                TypeMouvement.Sortie => _locale.T("TypeMvt_Sortie"),
                TypeMouvement.Ajustement => _locale.T("TypeMvt_Ajustement"),
                _ => m.Type.ToString()
            };
            return new ReportStockMovementRow(
                m.CreatedAt,
                m.Produit?.Reference ?? string.Empty,
                m.Produit?.Designation ?? string.Empty,
                typeStr,
                m.Quantite,
                m.OrigineType,
                m.StockApres);
        }).ToList();
    }

    public async Task<List<ReportProfitChargeRow>> GetProfitChargesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        var typeVente = _locale.T("Reports_TypeVente");
        var typeAvoir = _locale.T("Reports_TypeAvoir");
        var typeAchat = _locale.T("Reports_TypeAchat");
        var typeAvoirFournisseur = _locale.T("Reports_TypeAvoirFournisseur");
        var typeCharge = _locale.T("Reports_TypeCharge");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.ClientId,
                f.Numero,
                f.Date,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var avoirs = await db.Avoirs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .Select(a => new
            {
                a.ClientId,
                a.Numero,
                a.Date,
                a.RetourMarchandise,
                Lignes = a.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = factures.Select(f => f.ClientId)
            .Concat(avoirs.Select(a => a.ClientId))
            .Distinct()
            .ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        var allProdIds = factures.SelectMany(f => f.Lignes).Select(l => l.ProduitId)
            .Concat(avoirs.SelectMany(a => a.Lignes).Select(l => l.ProduitId))
            .Distinct()
            .ToList();
        var produits = await db.Produits.AsNoTracking()
            .Where(p => allProdIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PrixAchatHT })
            .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var rows = new List<ReportProfitChargeRow>();

        foreach (var f in factures.OrderBy(f => f.Date))
        {
            var factor = 1 - f.RemiseGlobale / 100m;
            decimal ht = 0, cost = 0;
            foreach (var l in f.Lignes)
            {
                var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
                var prixAchat = prodMap.GetValueOrDefault(l.ProduitId)?.PrixAchatHT ?? 0;
                ht += lht;
                cost += l.Quantite * prixAchat;
            }

            ht *= factor;
            cost *= factor;
            var profit = ht - cost;
            var client = clientMap.GetValueOrDefault(f.ClientId)?.Nom ?? string.Empty;
            var numero = string.IsNullOrWhiteSpace(f.Numero) ? string.Empty : f.Numero;
            var libelle = string.IsNullOrWhiteSpace(client)
                ? $"Facture {numero}".Trim()
                : $"Facture {numero} — {client}".Trim();

            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Vente,
                typeVente,
                libelle,
                f.Date,
                ht,
                profit,
                dev));
        }

        foreach (var a in avoirs.OrderBy(a => a.Date))
        {
            decimal ht = 0, cost = 0;
            foreach (var l in a.Lignes)
            {
                var lht = DocumentTotalsHelper.LigneHT(l.Quantite, l.PrixUnitaireHT, l.Remise);
                ht += lht;
                if (a.RetourMarchandise)
                {
                    var prixAchat = prodMap.GetValueOrDefault(l.ProduitId)?.PrixAchatHT ?? 0;
                    cost += l.Quantite * prixAchat;
                }
            }

            // Reverse sale margin: with stock return reverse HT−cost; commercial credit reverses HT only.
            var marginImpact = -(ht - cost);
            var client = clientMap.GetValueOrDefault(a.ClientId)?.Nom ?? string.Empty;
            var numero = string.IsNullOrWhiteSpace(a.Numero) ? string.Empty : a.Numero;
            var libelle = string.IsNullOrWhiteSpace(client)
                ? $"Avoir {numero}".Trim()
                : $"Avoir {numero} — {client}".Trim();

            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Avoir,
                typeAvoir,
                libelle,
                a.Date,
                ht,
                marginImpact,
                dev));
        }

        var facturesFournisseur = await db.FacturesFournisseurs.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.FournisseurId,
                f.Numero,
                f.Date,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var avoirsFournisseur = await db.AvoirsFournisseurs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .Select(a => new
            {
                a.FournisseurId,
                a.Numero,
                a.Date,
                Lignes = a.Lignes!.Select(l => new
                {
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        var fournisseurIds = facturesFournisseur.Select(f => f.FournisseurId)
            .Concat(avoirsFournisseur.Select(a => a.FournisseurId))
            .Distinct()
            .ToList();
        var fournisseurs = await db.Tiers.AsNoTracking()
            .Where(t => fournisseurIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var fournisseurMap = fournisseurs.ToDictionary(f => f.Id);

        foreach (var f in facturesFournisseur.OrderBy(f => f.Date))
        {
            var lignes = f.Lignes.Select(l => new FactureFournisseurLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            }).ToList();
            var (ht, _, ttc) = DocumentTotalsHelper.FactureFournisseurTotals(lignes, f.RemiseGlobale);
            var fournisseur = fournisseurMap.GetValueOrDefault(f.FournisseurId)?.Nom ?? string.Empty;
            var numero = string.IsNullOrWhiteSpace(f.Numero) ? string.Empty : f.Numero;
            var libelle = string.IsNullOrWhiteSpace(fournisseur)
                ? $"Facture {numero}".Trim()
                : $"Facture {numero} — {fournisseur}".Trim();

            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Achat,
                typeAchat,
                libelle,
                f.Date,
                ht,
                -ttc,
                dev));
        }

        foreach (var a in avoirsFournisseur.OrderBy(a => a.Date))
        {
            var lignes = a.Lignes.Select(l => new AvoirFournisseurLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            }).ToList();
            var (ht, _, ttc) = DocumentTotalsHelper.AvoirFournisseurTotals(lignes);
            var fournisseur = fournisseurMap.GetValueOrDefault(a.FournisseurId)?.Nom ?? string.Empty;
            var numero = string.IsNullOrWhiteSpace(a.Numero) ? string.Empty : a.Numero;
            var libelle = string.IsNullOrWhiteSpace(fournisseur)
                ? $"Avoir {numero}".Trim()
                : $"Avoir {numero} — {fournisseur}".Trim();

            // Opposite of supplier invoice (−TTC): credit reduces total achats.
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.AvoirFournisseur,
                typeAvoirFournisseur,
                libelle,
                a.Date,
                ht,
                ttc,
                dev));
        }

        var charges = await db.Charges.AsNoTracking()
            .Where(c => c.Date >= from && c.Date < toEnd)
            .OrderBy(c => c.Date)
            .Select(c => new { c.Date, c.Numero, c.Libelle, c.MontantTtc, c.CategorieChargeId })
            .ToListAsync(ct);

        var chargeCatIds = charges.Select(c => c.CategorieChargeId).Distinct().ToList();
        var chargeCategories = await db.CategoriesCharges.AsNoTracking()
            .Where(cat => chargeCatIds.Contains(cat.Id))
            .Select(cat => new { cat.Id, cat.Nom })
            .ToListAsync(ct);
        var chargeCatMap = chargeCategories.ToDictionary(c => c.Id);

        foreach (var c in charges)
        {
            var categorieNom = chargeCatMap.GetValueOrDefault(c.CategorieChargeId)?.Nom;
            var typeLabel = !string.IsNullOrWhiteSpace(categorieNom) ? categorieNom : typeCharge;
            var libelle = !string.IsNullOrWhiteSpace(c.Libelle)
                ? c.Libelle
                : c.Numero;
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Charge,
                typeLabel,
                libelle,
                c.Date,
                null,
                -c.MontantTtc,
                dev));
        }

        return rows
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Kind switch
            {
                ReportProfitChargeKind.Vente => 0,
                ReportProfitChargeKind.Avoir => 1,
                ReportProfitChargeKind.Achat => 2,
                ReportProfitChargeKind.AvoirFournisseur => 3,
                _ => 4
            })
            .ToList();
    }

    public async Task<(decimal ht, decimal ttc, string devise)> GetStockValuationAsync(CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var produits = await db.Produits.AsNoTracking()
            .Where(p => p.StockActuel > 0)
            .Select(p => new { p.StockActuel, p.PrixAchatHT, p.PrixVenteHT, p.TauxTVA })
            .ToListAsync(ct);

        decimal totalHt = 0, totalTtc = 0;
        foreach (var p in produits)
        {
            totalHt += p.StockActuel * p.PrixAchatHT;
            totalTtc += p.StockActuel * p.PrixVenteHT * (1 + p.TauxTVA / 100m);
        }
        return (totalHt, totalTtc, dev);
    }

    private async Task<string> GetDeviseAsync(CancellationToken ct = default)
    {
        var cfg = await _settings.GetAsync(ct);
        return string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise!;
    }
}
