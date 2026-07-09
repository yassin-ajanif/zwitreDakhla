using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Modules.Production.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
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
    private readonly IAppSettingsService _settings;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private bool _suppressFilterReload;

    public ProductionListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        ILocaleService locale,
        ICurrentUserSession session,
        IAppSettingsService settings,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _locale = locale;
        _session = session;
        _settings = settings;
        _workspace = workspaceNavigator;
        _sp = sp;
        (_dateFrom, _dateTo) = ThisYearRange();
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        Title = _locale.T("CmdProd_ListTitle");
        Pagination = new PaginationHelper(() => _ = LoadPageAsync(CancellationToken.None, reloadFilters: false));
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: true);
    }

    public PaginationHelper Pagination { get; }

    public ObservableCollection<CommandeProductionListItem> Commandes { get; } = [];
    public ObservableCollection<ProductionListFilterOption> FilterFournisseurs { get; } = [];
    public ObservableCollection<ProductionListFilterOption> FilterCategories { get; } = [];
    public ObservableCollection<ProductionListFilterOption> FilterTypes { get; } = [];

    [ObservableProperty] private CommandeProductionListItem? _selected;
    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _wmFilterSupplier = string.Empty;
    [ObservableProperty] private string _wmFilterCategorie = string.Empty;
    [ObservableProperty] private string _wmFilterType = string.Empty;
    [ObservableProperty] private string _wmFilterExpiration = string.Empty;
    [ObservableProperty] private string _lblExpirationFilterAll = string.Empty;
    [ObservableProperty] private string _lblExpirationFilterEnCours = string.Empty;
    [ObservableProperty] private string _lblExpirationFilterTerminee = string.Empty;
    [ObservableProperty] private string _btnResetFilters = string.Empty;
    [ObservableProperty] private string _wmSort = string.Empty;
    [ObservableProperty] private string _lblSortDefault = string.Empty;
    [ObservableProperty] private string _lblSortBestMortality = string.Empty;
    [ObservableProperty] private string _lblSortBestAgrandissement = string.Empty;
    [ObservableProperty] private string _lblSortBestCommande = string.Empty;
    /// <summary>0 = all, 1 = en cours, 2 = expirée (terminée).</summary>
    [ObservableProperty] private int _expirationFilterIndex;
    /// <summary>0 = date, 1 = lowest mortality, 2 = shortest growth, 3 = best quality factor.</summary>
    [ObservableProperty] private int _sortFilterIndex;
    [ObservableProperty] private ProductionListFilterOption? _selectedFilterFournisseur;
    [ObservableProperty] private ProductionListFilterOption? _selectedFilterCategorie;
    [ObservableProperty] private ProductionListFilterOption? _selectedFilterType;

    partial void OnSelectedFilterFournisseurChanged(ProductionListFilterOption? value)
    {
        if (_suppressFilterReload) return;
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: false);
    }

    partial void OnSelectedFilterCategorieChanged(ProductionListFilterOption? value)
    {
        if (_suppressFilterReload) return;
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: false);
    }

    partial void OnSelectedFilterTypeChanged(ProductionListFilterOption? value)
    {
        if (_suppressFilterReload) return;
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: false);
    }

    partial void OnExpirationFilterIndexChanged(int value)
    {
        if (_suppressFilterReload) return;
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: false);
    }

    partial void OnSortFilterIndexChanged(int value)
    {
        if (_suppressFilterReload) return;
        _ = LoadPageAsync(CancellationToken.None, resetPage: true, reloadFilters: false);
    }

    private void RefreshUi()
    {
        BtnNew = _locale.T("CmdProd_BtnNew");
        UpdateBtnFilterDateText();
        MenuDelete = _locale.T("CmdProd_MenuDelete");
        Title = _locale.T("CmdProd_ListTitle");
        WmFilterSupplier = _locale.T("CmdProd_FilterSupplier");
        WmFilterCategorie = _locale.T("CmdProd_FilterCategorie");
        WmFilterType = _locale.T("CmdProd_FilterType");
        WmFilterExpiration = _locale.T("CmdProd_FilterExpiration");
        LblExpirationFilterAll = _locale.T("CmdProd_ExpirationFilterAll");
        LblExpirationFilterEnCours = _locale.T("CmdProd_ExpirationFilterEnCours");
        LblExpirationFilterTerminee = _locale.T("CmdProd_ExpirationFilterTerminee");
        BtnResetFilters = _locale.T("CmdProd_BtnResetFilters");
        WmSort = _locale.T("CmdProd_SortLabel");
        LblSortDefault = _locale.T("CmdProd_SortDefault");
        LblSortBestMortality = _locale.T("CmdProd_SortBestMortality");
        LblSortBestAgrandissement = _locale.T("CmdProd_SortBestAgrandissement");
        LblSortBestCommande = _locale.T("CmdProd_SortBestCommande");
        UpdateFilterAllLabels();
        ApplyListLabels();
    }

    private void UpdateFilterAllLabels()
    {
        if (FilterFournisseurs.Count > 0)
            FilterFournisseurs[0] = ProductionListFilterOption.All(_locale.T("CmdProd_FilterSupplierAll"));
        if (FilterCategories.Count > 0)
            FilterCategories[0] = ProductionListFilterOption.All(_locale.T("CmdProd_FilterCategorieAll"));
        if (FilterTypes.Count > 0)
            FilterTypes[0] = ProductionListFilterOption.All(_locale.T("CmdProd_FilterTypeAll"));
    }

    private async Task LoadFiltersAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var fournisseurId = SelectedFilterFournisseur?.Id;
        var categorieId = SelectedFilterCategorie?.Id;
        var typeId = SelectedFilterType?.Id;

        _suppressFilterReload = true;
        try
        {
            FilterFournisseurs.Clear();
            FilterFournisseurs.Add(ProductionListFilterOption.All(_locale.T("CmdProd_FilterSupplierAll")));
            var fournisseurs = await db.Tiers.AsNoTracking()
                .Where(t => t.Actif && (t.Type == TypeTiers.Fournisseur || t.Type == TypeTiers.LesDeux))
                .OrderBy(t => t.Nom)
                .ToListAsync(cancellationToken);
            foreach (var row in fournisseurs)
                FilterFournisseurs.Add(ProductionListFilterOption.From(row.Id, row.Nom));

            FilterCategories.Clear();
            FilterCategories.Add(ProductionListFilterOption.All(_locale.T("CmdProd_FilterCategorieAll")));
            var categories = await db.CategoriesCommande.AsNoTracking()
                .Where(c => c.Actif)
                .OrderBy(c => c.Ordre)
                .ThenBy(c => c.Nom)
                .ToListAsync(cancellationToken);
            foreach (var row in categories)
                FilterCategories.Add(ProductionListFilterOption.From(row.Id, row.Nom));

            FilterTypes.Clear();
            FilterTypes.Add(ProductionListFilterOption.All(_locale.T("CmdProd_FilterTypeAll")));
            var types = await db.TypesHuitre.AsNoTracking()
                .Where(t => t.Actif)
                .OrderBy(t => t.Ordre)
                .ThenBy(t => t.Nom)
                .ToListAsync(cancellationToken);
            foreach (var row in types)
                FilterTypes.Add(ProductionListFilterOption.From(row.Id, row.Nom));

            SelectedFilterFournisseur = FindFilterOption(FilterFournisseurs, fournisseurId);
            SelectedFilterCategorie = FindFilterOption(FilterCategories, categorieId);
            SelectedFilterType = FindFilterOption(FilterTypes, typeId);
        }
        finally
        {
            _suppressFilterReload = false;
        }
    }

    private static ProductionListFilterOption FindFilterOption(
        IEnumerable<ProductionListFilterOption> options,
        int? id) =>
        options.FirstOrDefault(o => o.Id == id) ?? options.First();

    private void ApplyListLabels()
    {
        foreach (var item in Commandes)
            ApplyItemLabels(item);
    }

    private void ApplyItemLabels(CommandeProductionListItem item)
    {
        item.EtatLabel = item.EstTerminee
            ? _locale.T("CmdProd_Terminee")
            : _locale.T("CmdProd_EnCours");
        item.NaissainChipPrefix = _locale.T("CmdProd_ChipNaissainPrefix");
        item.MortaliteChipLabel = _locale.Tf("CmdProd_ChipMortaliteFmt", item.TauxMortaliteLabel);
        item.AgrandissementChipLabel = _locale.Tf("CmdProd_ChipAgrandissementFmt", item.TauxAgrandissementLabel);
        item.OperationsChipLabel = _locale.Tf("CmdProd_ChipOperationsFmt", item.OperationCountLabel);
        item.WaterOrDeadHuitresChipLabel = item.EstTerminee
            ? _locale.Tf("CmdProd_ChipHuitresMortesFmt", item.RestantOuMortesHuitresLabel)
            : _locale.Tf("CmdProd_ChipRestantEauFmt", item.RestantOuMortesHuitresLabel);
        item.ExpirationChipLabel = item.ShowExpirationChip
            ? _locale.Tf("CmdProd_ChipExpirationFmt", item.DateExpirationLabel)
            : string.Empty;

        item.SummaryLine2 = item.EstTerminee
            ? _locale.Tf(
                "CmdProd_SummaryLine2TermineeFmt",
                item.CategorieCommandeNom,
                item.TypeHuitreNom,
                item.QuantiteNaissainLabel,
                item.TauxMortaliteLabel)
            : _locale.Tf(
                "CmdProd_SummaryLine2EnCoursFmt",
                item.CategorieCommandeNom,
                item.TypeHuitreNom,
                item.QuantiteNaissainLabel);
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
            BtnFilterDate = IsThisYearRange(from, to)
                ? _locale.T("Btn_ThisYear")
                : from.Date == DateTime.Today && to.Date == DateTime.Today
                    ? _locale.T("Btn_Today")
                    : $"{from:dd/MM/yy} — {to:dd/MM/yy}";
        }
        else
        {
            BtnFilterDate = _locale.T("Btn_FilterDate");
        }
    }

    private static (DateTime From, DateTime To) ThisYearRange()
    {
        var today = DateTime.Today;
        return (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31));
    }

    private static bool IsThisYearRange(DateTime from, DateTime to)
    {
        var (yearFrom, yearTo) = ThisYearRange();
        return from.Date == yearFrom && to.Date == yearTo;
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken, bool resetPage = false, bool reloadFilters = false)
    {
        if (!_session.CanAccessProduction)
        {
            Commandes.Clear();
            Pagination.TotalCount = 0;
            return;
        }

        IsBusy = true;
        try
        {
            if (resetPage)
                Pagination.CurrentPage = 1;

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (reloadFilters)
                await LoadFiltersAsync(db, cancellationToken);

            var q = BuildFilteredQuery(db);
            Pagination.TotalCount = await q.CountAsync(cancellationToken);

            var projected = ProjectQuery(q);
            var settings = await _settings.GetAsync(cancellationToken);
            var selId = Selected?.Id;

            List<ProductionListQueryRow> pageRows;
            if (SortFilterIndex == 0)
            {
                pageRows = await projected
                    .OrderByDescending(c => c.DateCommande)
                    .ThenByDescending(c => c.Id)
                    .Skip(Pagination.Skip)
                    .Take(Pagination.PageSize)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var allRows = await projected.ToListAsync(cancellationToken);
                var sorted = SortFilterIndex switch
                {
                    1 => allRows
                        .OrderBy(i => i.EstTerminee ? ComputeTauxMortalite(i) : decimal.MaxValue)
                        .ThenByDescending(i => i.DateCommande)
                        .ToList(),
                    2 => allRows
                        .OrderBy(i => i.DureeAgrandissementJours ?? int.MaxValue)
                        .ThenByDescending(i => i.DateCommande)
                        .ToList(),
                    3 => allRows
                        .OrderByDescending(i => ComputeFacteurQualite(i, settings) ?? -1m)
                        .ThenByDescending(i => i.DateCommande)
                        .ToList(),
                    _ => allRows
                        .OrderByDescending(i => i.DateCommande)
                        .ThenByDescending(i => i.Id)
                        .ToList()
                };

                pageRows = sorted
                    .Skip(Pagination.Skip)
                    .Take(Pagination.PageSize)
                    .ToList();
            }

            Commandes.Clear();
            foreach (var row in pageRows)
            {
                var item = MapQueryRow(row, settings);
                ApplyItemLabels(item);
                Commandes.Add(item);
            }

            if (selId is { } id)
                Selected = Commandes.FirstOrDefault(x => x.Id == id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private IQueryable<CommandeProduction> BuildFilteredQuery(AppDbContext db)
    {
        var q = db.CommandesProduction.AsNoTracking().AsQueryable();

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

        if (SelectedFilterFournisseur?.Id is int fournisseurId)
            q = q.Where(c => c.FournisseurId == fournisseurId);

        if (SelectedFilterCategorie?.Id is int categorieId)
            q = q.Where(c => c.CategorieCommandeId == categorieId);

        if (SelectedFilterType?.Id is int typeId)
            q = q.Where(c => c.TypeHuitreId == typeId);

        return ExpirationFilterIndex switch
        {
            1 => q.Where(c => !c.EstTerminee),
            2 => q.Where(c => c.EstTerminee),
            _ => q
        };
    }

    private static IQueryable<ProductionListQueryRow> ProjectQuery(IQueryable<CommandeProduction> q) =>
        q.Select(c => new ProductionListQueryRow
        {
            Id = c.Id,
            Numero = c.Numero,
            FournisseurNom = c.Fournisseur != null ? c.Fournisseur.Nom : "—",
            DateCommande = c.DateCommande,
            CategorieCommandeNom = c.CategorieCommande != null ? c.CategorieCommande.Nom : "—",
            TypeHuitreNom = c.TypeHuitre != null ? c.TypeHuitre.Nom : "—",
            QuantiteNaissain = c.QuantiteNaissain,
            EstTerminee = c.EstTerminee,
            DateExpiration = c.DateExpiration,
            OperationCount = c.Operations.Count,
            SumGrandHuitres = c.Operations.Sum(o => o.PochetteGrand * ProductionOperation.MultiplierGrand),
            TotalHuitres = c.Operations.Sum(o =>
                o.PochetteGrand * ProductionOperation.MultiplierGrand
                + o.PochetteMoyenne * ProductionOperation.MultiplierMoyenne
                + o.PochettePetit * ProductionOperation.MultiplierPetit),
            LastOperationAt = c.Operations.Select(o => (DateTime?)o.OperationAt).Max()
        });

    private CommandeProductionListItem MapQueryRow(ProductionListQueryRow row, AppSettingsRow settings)
    {
        var item = new CommandeProductionListItem
        {
            Id = row.Id,
            Numero = row.Numero,
            FournisseurNom = row.FournisseurNom,
            DateCommande = row.DateCommande,
            CategorieCommandeNom = row.CategorieCommandeNom,
            TypeHuitreNom = row.TypeHuitreNom,
            QuantiteNaissain = row.QuantiteNaissain,
            EstTerminee = row.EstTerminee,
            DateExpiration = row.DateExpiration,
            DureeAgrandissementJours = row.DureeAgrandissementJours,
            TauxMortalite = row.EstTerminee ? ComputeTauxMortalite(row) : 0,
            SumGrandHuitres = row.SumGrandHuitres,
            OperationCount = row.OperationCount,
            TotalHuitres = row.TotalHuitres,
            LastOperationAt = row.LastOperationAt
        };
        item.FacteurQualite = ComputeFacteurQualite(row, settings);
        item.FacteurChipLabel = _locale.Tf("CmdProd_ChipFacteurFmt", item.FacteurQualiteLabel);
        return item;
    }

    private static decimal ComputeTauxMortalite(ProductionListQueryRow row) =>
        ProductionOperation.ComputeTauxMortalitePercent(row.QuantiteNaissain, row.SumGrandHuitres);

    private static decimal? ComputeFacteurQualite(ProductionListQueryRow row, AppSettingsRow settings) =>
        ProductionQualityScore.ComputeFacteurQualite(
            row.EstTerminee ? ComputeTauxMortalite(row) : 0,
            row.DureeAgrandissementJours,
            settings.ImportanceTauxMortalite,
            settings.ImportanceTauxAgrandissement,
            settings.AgrandissementMaxJours,
            row.EstTerminee);

    private sealed class ProductionListQueryRow
    {
        public int Id { get; init; }
        public string Numero { get; init; } = string.Empty;
        public string FournisseurNom { get; init; } = string.Empty;
        public DateTime DateCommande { get; init; }
        public string CategorieCommandeNom { get; init; } = string.Empty;
        public string TypeHuitreNom { get; init; } = string.Empty;
        public int QuantiteNaissain { get; init; }
        public bool EstTerminee { get; init; }
        public DateTime? DateExpiration { get; init; }
        public int OperationCount { get; init; }
        public int SumGrandHuitres { get; init; }
        public int TotalHuitres { get; init; }
        public DateTime? LastOperationAt { get; init; }

        public int? DureeAgrandissementJours => EstTerminee
            ? ProductionOperation.ComputeDureeAgrandissementJours(DateCommande, DateExpiration)
            : null;
    }

    private async Task LoadAsync(CancellationToken cancellationToken, bool reloadFilters = true) =>
        await LoadPageAsync(cancellationToken, resetPage: true, reloadFilters);

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

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.CommandesProduction
                .Include(c => c.Operations)
                .FirstOrDefaultAsync(c => c.Id == item.Id, cancellationToken);
            if (entity == null) return;

            var productionStock = _sp.GetRequiredService<IProductionStockService>();
            var brId = entity.BonReceptionId;
            await productionStock.RemoveCommandeStockAsync(
                db,
                entity.Id,
                entity.Operations,
                _session.UserId,
                cancellationToken);

            db.CommandesProduction.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);

            await productionStock.RemoveLinkedBonReceptionAsync(db, brId, cancellationToken);
            await LoadPageAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowExceptionAsync(Title, ex, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
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
            (_dateFrom, _dateTo) = ThisYearRange();
        }
        else
        {
            _dateFrom = range.Value.from.Date;
            _dateTo = range.Value.to.Date;
        }

        UpdateBtnFilterDateText();
        await LoadPageAsync(cancellationToken, resetPage: true);
    }

    [RelayCommand]
    private async Task ResetFiltersAsync(CancellationToken cancellationToken)
    {
        _suppressFilterReload = true;
        try
        {
            ExpirationFilterIndex = 0;
            SortFilterIndex = 0;
            if (FilterFournisseurs.Count > 0)
                SelectedFilterFournisseur = FilterFournisseurs[0];
            if (FilterCategories.Count > 0)
                SelectedFilterCategorie = FilterCategories[0];
            if (FilterTypes.Count > 0)
                SelectedFilterType = FilterTypes[0];
            (_dateFrom, _dateTo) = ThisYearRange();
            UpdateBtnFilterDateText();
        }
        finally
        {
            _suppressFilterReload = false;
        }

        await LoadPageAsync(cancellationToken, resetPage: true, reloadFilters: false);
    }
}
