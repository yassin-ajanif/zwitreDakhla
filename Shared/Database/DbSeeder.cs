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

        var zwittre = db.Produits.FirstOrDefault(p => p.Reference == ProductionStockService.ZwittreGrandReference);
        if (zwittre == null)
        {
            db.Produits.Add(new Produit
            {
                Reference = ProductionStockService.ZwittreGrandReference,
                Designation = ProductionStockService.ZwittreGrandDesignation,
                Unite = "U",
                TauxTVA = 20,
                Actif = true
            });
            db.SaveChanges();
        }
        else if (zwittre.TauxTVA == 0)
        {
            zwittre.TauxTVA = 20;
            db.SaveChanges();
        }
    }
}
