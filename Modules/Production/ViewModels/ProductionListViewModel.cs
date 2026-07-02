using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Production.ViewModels;

public partial class ProductionListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly ICurrentUserSession _session;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    public ProductionListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        ILocaleService locale,
        ICurrentUserSession session,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _locale = locale;
        _session = session;
        _workspace = workspaceNavigator;
        _sp = sp;
        _dateFrom = DateTime.Today;
        _dateTo = DateTime.Today;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        Title = _locale.T("CmdProd_ListTitle");
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<CommandeProductionListItem> Commandes { get; } = [];

    [ObservableProperty] private CommandeProductionListItem? _selected;
    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;

    private void RefreshUi()
    {
        BtnNew = _locale.T("CmdProd_BtnNew");
        UpdateBtnFilterDateText();
        MenuDelete = _locale.T("CmdProd_MenuDelete");
        Title = _locale.T("CmdProd_ListTitle");
        ApplyListLabels();
    }

    private void ApplyListLabels()
    {
        foreach (var item in Commandes)
            ApplyItemLabels(item);
    }

    private void ApplyItemLabels(CommandeProductionListItem item)
    {
        item.SummaryLine2 = _locale.Tf(
            "CmdProd_SummaryLine2Fmt",
            item.CategorieCommandeNom,
            item.TypeNaissainNom,
            item.QuantiteNaissainLabel,
            item.TauxMortaliteLabel);
        item.SummaryLine3 = _locale.Tf(
            "CmdProd_SummaryLine3Fmt",
            item.OperationCountLabel,
            item.TotalHuitresLabel,
            item.LastOperationLabel);
    }

    private void UpdateBtnFilterDateText()
    {
        if (_dateFrom is { } from && _dateTo is { } to)
        {
            BtnFilterDate = from.Date == DateTime.Today && to.Date == DateTime.Today
                ? _locale.T("Btn_Today")
                : $"{from:dd/MM/yy} — {to:dd/MM/yy}";
        }
        else
        {
            BtnFilterDate = _locale.T("Btn_FilterDate");
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction)
        {
            Commandes.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var q = db.CommandesProduction.AsNoTracking()
                .Include(c => c.Fournisseur)
                .Include(c => c.CategorieCommande)
                .Include(c => c.TypeNaissain)
                .Include(c => c.Operations)
                .AsQueryable();

            if (_dateFrom.HasValue)
            {
                var from = _dateFrom.Value.Date;
                q = q.Where(c => c.DateCommande >= from);
            }

            if (_dateTo.HasValue)
            {
                var toExclusive = _dateTo.Value.Date.AddDays(1);
                q = q.Where(c => c.DateCommande < toExclusive);
            }

            var rows = await q
                .OrderByDescending(c => c.DateCommande)
                .ThenByDescending(c => c.Id)
                .ToListAsync(cancellationToken);

            Commandes.Clear();
            foreach (var row in rows)
            {
                var item = new CommandeProductionListItem
                {
                    Id = row.Id,
                    Numero = row.Numero,
                    FournisseurNom = row.Fournisseur?.Nom ?? "—",
                    DateCommande = row.DateCommande,
                    CategorieCommandeNom = row.CategorieCommande?.Nom ?? "—",
                    TypeNaissainNom = row.TypeNaissain?.Nom ?? "—",
                    QuantiteNaissain = row.QuantiteNaissain,
                    TauxMortalite = ProductionOperation.ComputeTauxMortalitePercent(
                        row.QuantiteNaissain,
                        ProductionOperation.SumGrandHuitres(row.Operations)),
                    OperationCount = row.Operations.Count,
                    TotalHuitres = row.Operations.Sum(o =>
                        o.PochetteGrand * ProductionOperation.MultiplierGrand
                        + o.PochetteMoyenne * ProductionOperation.MultiplierMoyenne
                        + o.PochettePetit * ProductionOperation.MultiplierPetit),
                    LastOperationAt = row.Operations.Count > 0
                        ? row.Operations.Max(o => o.OperationAt)
                        : null
                };
                ApplyItemLabels(item);
                Commandes.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewCommande()
    {
        if (!_session.CanAccessProduction) return;
        var vm = _sp.GetRequiredService<CommandeProductionEditViewModel>();
        vm.LoadNew();
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (Selected == null) return;
        var vm = _sp.GetRequiredService<CommandeProductionEditViewModel>();
        vm.Load(Selected.Id);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task DeleteCommandeAsync(CommandeProductionListItem? item, CancellationToken cancellationToken)
    {
        if (item == null || !_session.CanAccessProduction) return;

        var ok = await _dialog.ConfirmAsync(
            Title,
            _locale.Tf("CmdProd_ConfirmDelete", item.Numero),
            cancellationToken);
        if (!ok) return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.CommandesProduction
            .Include(c => c.Operations)
            .FirstOrDefaultAsync(c => c.Id == item.Id, cancellationToken);
        if (entity == null) return;

        db.CommandesProduction.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(
            _locale.T("Btn_FilterDate"),
            cancellationToken,
            _dateFrom,
            _dateTo);
        if (range == null) return;

        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _dateFrom = DateTime.Today;
            _dateTo = DateTime.Today;
        }
        else
        {
            _dateFrom = range.Value.from.Date;
            _dateTo = range.Value.to.Date;
        }

        UpdateBtnFilterDateText();
        await LoadAsync(cancellationToken);
    }
}
