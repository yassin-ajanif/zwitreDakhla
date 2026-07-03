namespace GestionCommerciale.Shared.Database;

using GestionCommerciale.Modules.Production.Services;
using GestionCommerciale.Modules.Stock.Models;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@local";
    public const string DefaultAdminPassword = "admin";
    public const string DefaultClientName = "Client Comptoire";

    public static void Seed(AppDbContext db)
    {
        if (!db.AppSettings.Any())
        {
            db.AppSettings.Add(new AppSettingsRow { Id = 1 });
            db.SaveChanges();
        }

        if (!db.Tiers.Any(t => t.Nom == DefaultClientName))
        {
            db.Tiers.Add(new GestionCommerciale.Modules.Tiers.Models.Tiers
            {
                Nom = DefaultClientName,
                Type = GestionCommerciale.Modules.Tiers.Models.TypeTiers.Client,
                Actif = true
            });
            db.SaveChanges();
        }

        var legacyHuitre = db.Produits.FirstOrDefault(p =>
            p.Reference == ProductionStockService.LegacyHuitreGrandReference);
        if (legacyHuitre != null)
        {
            legacyHuitre.Reference = ProductionStockService.HuitreGrandReference;
            legacyHuitre.Designation = ProductionStockService.HuitreGrandDesignation;
            db.SaveChanges();
        }

        var huitre = db.Produits.FirstOrDefault(p => p.Reference == ProductionStockService.HuitreGrandReference);
        if (huitre == null)
        {
            db.Produits.Add(new Produit
            {
                Reference = ProductionStockService.HuitreGrandReference,
                Designation = ProductionStockService.HuitreGrandDesignation,
                Unite = "U",
                TauxTVA = 20,
                Actif = true
            });
            db.SaveChanges();
        }
        else
        {
            var changed = false;
            if (huitre.Designation != ProductionStockService.HuitreGrandDesignation)
            {
                huitre.Designation = ProductionStockService.HuitreGrandDesignation;
                changed = true;
            }
            if (huitre.TauxTVA == 0)
            {
                huitre.TauxTVA = 20;
                changed = true;
            }
            if (changed)
                db.SaveChanges();
        }

        var naissain = db.Produits.FirstOrDefault(p => p.Reference == ProductionStockService.NaissainReference);
        if (naissain == null)
        {
            db.Produits.Add(new Produit
            {
                Reference = ProductionStockService.NaissainReference,
                Designation = ProductionStockService.NaissainDesignation,
                Unite = "U",
                TauxTVA = 20,
                Actif = true
            });
            db.SaveChanges();
        }
        else if (naissain.Designation != ProductionStockService.NaissainDesignation)
        {
            naissain.Designation = ProductionStockService.NaissainDesignation;
            db.SaveChanges();
        }
    }
}
