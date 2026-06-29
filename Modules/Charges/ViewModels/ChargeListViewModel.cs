using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using GestionCommerciale.Modules.Tiers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ChargeListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly ICurrentUserSession _session;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    public ChargeListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        IDialogService dialog,
        ILocaleService locale,
        IAppSettingsService settings,
        ICurrentUserSession session)
    {
        _dbFactory = dbFactory;
        _workspace = workspaceNavigator;
        _sp = sp;
        _dialog = dialog;
        _locale = locale;
        _settings = settings;
        _session = session;
        _locale.CultureApplied += (_, _) => RefreshListToolbar();
        RefreshListToolbar();
        Title = _locale.T("ChargeList_Title");
        Pagination = new PaginationHelper(() => _ = LoadPageAsync(CancellationToken.None));
    }

    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _menuDeleteCharge = string.Empty;
    [ObservableProperty] private string _colHeaderRef = string.Empty;
    [ObservableProperty] private string _colHeaderCategorie = string.Empty;
    [ObservableProperty] private string _colHeaderBeneficiaire = string.Empty;
    [ObservableProperty] private string _colHeaderDate = string.Empty;
    [ObservableProperty] private string _colHeaderMontant = string.Empty;
    [ObservableProperty] private string _colHeaderLibelle = string.Empty;
    [ObservableProperty] private string _searchWatermark = string.Empty;
    public PaginationHelper Pagination { get; }

    public ObservableCollection<ChargeListRow> Items { get; } = [];
    [ObservableProperty] private ChargeListRow? _selected;
    [ObservableProperty] private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => _ = LoadPageAsync(CancellationToken.None, true);

    private void RefreshListToolbar()
    {
        BtnNew = _locale.T("Btn_NewCharge");
        UpdateBtnFilterDateText();
        MenuDeleteCharge = _locale.T("Chg_MenuDelete");
        ColHeaderRef = _locale.T("DevisList_ColRef");
        ColHeaderCategorie = _locale.T("Chg_ColCategorie");
        ColHeaderBeneficiaire = _locale.T("Chg_ColBeneficiaire");
        ColHeaderDate = _locale.T("DevisList_ColDate");
        ColHeaderMontant = _locale.T("DevisList_ColTtc");
        ColHeaderLibelle = _locale.T("Chg_ColLibelle");
        SearchWatermark = _locale.T("Wm_SearchCharges");
        Title = _locale.T("ChargeList_Title");
    }

    private async Task LoadPageAsync(CancellationToken ct, bool resetPage = false)
    {
        if (!_session.CanAccessCharges)
        {
            Items.Clear();
            Pagination.TotalCount = 0;
            return;
        }

        IsBusy = true;
        try
        {
            if (resetPage)
                Pagination.CurrentPage = 1;

            var cfg = await _settings.GetAsync(ct);
            var devise = string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise.Trim();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var q = db.Charges.AsNoTracking().AsQueryable();
            if (_dateFrom.HasValue)
                q = q.Where(c => c.Date >= _dateFrom.Value);
            if (_dateTo.HasValue)
                q = q.Where(c => c.Date <= _dateTo.Value);

            var search = SearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(c =>
                    EF.Functions.Like(c.Numero, $"%{search}%")
                    || EF.Functions.Like(c.Libelle, $"%{search}%")
                    || EF.Functions.Like(c.Fournisseur, $"%{search}%")
                    || db.CategoriesCharges.AsNoTracking().Any(cat =>
                        cat.Id == c.CategorieChargeId && EF.Functions.Like(cat.Nom, $"%{search}%"))
                    || (c.FournisseurId != null && db.Tiers.AsNoTracking().Any(t =>
                        t.Id == c.FournisseurId && EF.Functions.Like(t.Nom, $"%{search}%"))));
            }

            var total = await q.CountAsync(ct);
            var list = await q.OrderByDescending(c => c.Date).ThenByDescending(c => c.Id)
                .Skip(Pagination.Skip).Take(Pagination.PageSize)
                .ToListAsync(ct);

            var catIds = list.Select(c => c.CategorieChargeId).Distinct().ToList();
            var cats = await db.CategoriesCharges.AsNoTracking()
                .Where(c => catIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Nom, ct);

            var fournisseurIds = list.Where(c => c.FournisseurId.HasValue).Select(c => c.FournisseurId!.Value).Distinct().ToList();
            var fournisseurs = await db.Tiers.AsNoTracking()
                .Where(t => fournisseurIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, ct);

            var selId = Selected?.Charge.Id;
            Items.Clear();
            foreach (var c in list)
            {
                var beneficiaire = !string.IsNullOrWhiteSpace(c.Fournisseur)
                    ? c.Fournisseur
                    : (c.FournisseurId is { } fid ? fournisseurs.GetValueOrDefault(fid) ?? string.Empty : string.Empty);
                Items.Add(ChargeListRow.Create(
                    c,
                    cats.GetValueOrDefault(c.CategorieChargeId) ?? string.Empty,
                    beneficiaire,
                    devise));
            }

            Pagination.TotalCount = total;
            if (selId is { } id)
                Selected = Items.FirstOrDefault(x => x.Charge.Id == id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken ct) => LoadPageAsync(ct, true);

    private void UpdateBtnFilterDateText()
    {
        if (_dateFrom.HasValue && _dateTo.HasValue)
            BtnFilterDate = $"{_dateFrom:dd/MM/yy} — {_dateTo:dd/MM/yy}";
        else
            BtnFilterDate = _locale.T("Btn_FilterDate");
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null) return;
        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _dateFrom = null;
            _dateTo = null;
        }
        else
        {
            _dateFrom = range.Value.from;
            _dateTo = range.Value.to;
        }
        UpdateBtnFilterDateText();
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task NewChargeAsync(CancellationToken cancellationToken)
    {
        var vm = _sp.GetRequiredService<ChargeEditViewModel>();
        await vm.LoadAsync(null, cancellationToken);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task OpenSelectedAsync(CancellationToken cancellationToken)
    {
        var sel = Selected;
        if (sel == null) return;
        var vm = _sp.GetRequiredService<ChargeEditViewModel>();
        await vm.LoadAsync(sel.Charge.Id, cancellationToken);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task DeleteChargeAsync(ChargeListRow? row, CancellationToken cancellationToken)
    {
        if (row == null) return;
        var ok = await _dialog.ConfirmAsync(
            _locale.T("Chg_Title"),
            _locale.Tf("Chg_ConfirmDelete", row.Charge.Numero),
            cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.Charges.FirstOrDefaultAsync(c => c.Id == row.Charge.Id, cancellationToken);
            if (entity == null) return;
            db.Charges.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await LoadPageAsync(cancellationToken, true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
