using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Reporting.ViewModels;

public sealed class ReportSaleByProductRow
{
    public ReportSaleByProductRow(string reference, string designation, string categorie,
        decimal quantite, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Reference = reference;
        Designation = designation;
        Categorie = categorie;
        Quantite = quantite;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblQty = quantite.ToString("N2");
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Reference { get; }
    public string Designation { get; }
    public string Categorie { get; }
    public decimal Quantite { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblQty { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed class ReportSaleByCustomerProductRow
{
    public ReportSaleByCustomerProductRow(string reference, string designation,
        decimal quantite, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Reference = reference;
        Designation = designation;
        Quantite = quantite;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblQty = quantite.ToString("N2");
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Reference { get; }
    public string Designation { get; }
    public decimal Quantite { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblQty { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed partial class ReportSaleByCustomerRow : ObservableObject
{
    public ReportSaleByCustomerRow(string client, string ice, string ville,
        int nbFactures, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        List<ReportSaleByCustomerProductRow>? products = null)
    {
        Client = client;
        Ice = ice;
        Ville = ville;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (products != null)
        {
            foreach (var p in products)
                _products.Add(p);
        }
    }

    public string Client { get; }
    public string Ice { get; }
    public string Ville { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportSaleByCustomerProductRow> _products = [];
    public ObservableCollection<ReportSaleByCustomerProductRow> Products => _products;
}

public sealed class ReportRefundRow
{
    public ReportRefundRow(string numero, DateTime date, string client,
        string motif, bool retourMarchandise, decimal totalTtc, string devise)
    {
        Numero = numero;
        Date = date;
        Client = client;
        Motif = motif;
        RetourMarchandise = retourMarchandise;
        TotalTtc = totalTtc;
        Devise = devise;
        LblDate = date.ToString("d");
        LblTotal = $"{totalTtc:N2} {devise}";
        LblRetour = retourMarchandise ? "\u2713" : "";
    }

    public string Numero { get; }
    public DateTime Date { get; }
    public string Client { get; }
    public string Motif { get; }
    public bool RetourMarchandise { get; }
    public decimal TotalTtc { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblTotal { get; }
    public string LblRetour { get; }
}

public sealed class ReportDailySaleDetailRow
{
    public ReportDailySaleDetailRow(string numero, string client,
        decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Numero = numero;
        Client = client;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Numero { get; }
    public string Client { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed partial class ReportDailySaleRow : ObservableObject
{
    public ReportDailySaleRow(DateTime date, int nbFactures,
        decimal totalHt, decimal totalTva, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        List<ReportDailySaleDetailRow>? details = null)
    {
        Date = date;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTva = totalTva;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblDate = date.ToString("d");
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTva = $"{totalTva:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (details != null)
        {
            foreach (var d in details)
                _details.Add(d);
        }
    }

    public DateTime Date { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTva { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTva { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportDailySaleDetailRow> _details = [];
    public ObservableCollection<ReportDailySaleDetailRow> Details => _details;
}

public enum ReportProfitChargeKind
{
    Vente,
    Avoir,
    Achat,
    AvoirFournisseur,
    Charge
}

public sealed class ReportProfitChargeRow
{
    public ReportProfitChargeRow(
        ReportProfitChargeKind kind,
        string typeLabel,
        string libelle,
        DateTime date,
        decimal? montantHt,
        decimal signedAmount,
        string devise)
    {
        Kind = kind;
        TypeLabel = typeLabel;
        Libelle = libelle;
        Date = date;
        MontantHt = montantHt;
        SignedAmount = signedAmount;
        Devise = devise;
        LblDate = date.ToString("dd/MM/yyyy");
        LblMontantHt = montantHt.HasValue ? $"{montantHt.Value:N0} {devise}" : "—";
        var prefix = signedAmount >= 0 ? "+" : "";
        LblAmount = $"{prefix}{signedAmount:N0} {devise}";
    }

    /// <summary>Positive = green type label (sales / supplier credit); negative = red (purchases / client credit / charges).</summary>
    public decimal ColorSignal => Kind switch
    {
        ReportProfitChargeKind.Vente => 1m,
        ReportProfitChargeKind.AvoirFournisseur => 1m,
        _ => -1m
    };

    public ReportProfitChargeKind Kind { get; }
    public bool IsVente => Kind == ReportProfitChargeKind.Vente;
    public string TypeLabel { get; }
    public string Libelle { get; }
    public DateTime Date { get; }
    public decimal? MontantHt { get; }
    public decimal SignedAmount { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblMontantHt { get; }
    public string LblAmount { get; }
}

public sealed class ReportStockValueByProductRow
{
    public ReportStockValueByProductRow(
        string reference,
        string designation,
        decimal stockActuel,
        decimal prixAchatHt,
        decimal valeurHt,
        decimal valeurTtc,
        string devise)
    {
        Reference = reference;
        Designation = designation;
        StockActuel = stockActuel;
        PrixAchatHt = prixAchatHt;
        ValeurHt = valeurHt;
        ValeurTtc = valeurTtc;
        Devise = devise;
        LblStock = stockActuel.ToString("N2");
        LblPrixAchatHt = $"{prixAchatHt:N2} {devise}";
        LblValeurHt = $"{valeurHt:N2} {devise}";
        LblValeurTtc = $"{valeurTtc:N2} {devise}";
    }

    public string Reference { get; }
    public string Designation { get; }
    public decimal StockActuel { get; }
    public decimal PrixAchatHt { get; }
    public decimal ValeurHt { get; }
    public decimal ValeurTtc { get; }
    public string Devise { get; }
    public string LblStock { get; }
    public string LblPrixAchatHt { get; }
    public string LblValeurHt { get; }
    public string LblValeurTtc { get; }
}
