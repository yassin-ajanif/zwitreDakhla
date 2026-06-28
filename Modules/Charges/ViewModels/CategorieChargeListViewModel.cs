using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class CategorieChargeListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly ICurrentUserSession _session;
    private CancellationTokenSource? _filterLoadCts;
    private int _loadGeneration;

    public PaginationHelper Pagination { get; }

    public CategorieChargeListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        ILocaleService locale,
        ICurrentUserSession session)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _locale = locale;
        _session = session;
        _locale.CultureApplied += (_, _) => RefreshListUi();
        Pagination = new PaginationHelper(() => _ = LoadItemsAsync(CancellationToken.None));
        RefreshListUi();
        Title = _locale.T("CategorieChargeList_Title");
    }

    [ObservableProperty] private string _wmSearch = string.Empty;
    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnToggleActif = string.Empty;
    [ObservableProperty] private string _colNom = string.Empty;
    [ObservableProperty] private string _colActif = string.Empty;

    public ObservableCollection<CategorieCharge> Items { get; } = [];
    [ObservableProperty] private string _filter = string.Empty;
    [ObservableProperty] private CategorieCharge? _selected;

    partial void OnFilterChanged(string value)
    {
        _filterLoadCts?.Cancel();
        _filterLoadCts?.Dispose();
        _filterLoadCts = new CancellationTokenSource();
        _ = LoadItemsAsync(_filterLoadCts.Token, resetPagination: true);
    }

    private void RefreshListUi()
    {
        WmSearch = _locale.T("Wm_SearchCategorieCharge");
        BtnNew = _locale.T("Btn_New");
        BtnToggleActif = _locale.T("Btn_ToggleActif");
        ColNom = _locale.T("Lbl_ColNom");
        ColActif = _locale.T("Lbl_ColActif");
        Title = _locale.T("CategorieChargeList_Title");
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken) => LoadItemsAsync(cancellationToken);

    private async Task LoadItemsAsync(CancellationToken cancellationToken, bool resetPagination = false)
    {
        if (!_session.CanAccessCharges)
        {
            Items.Clear();
            Pagination.TotalCount = 0;
            return;
        }

        var generation = Interlocked.Increment(ref _loadGeneration);
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var q = db.CategoriesCharges.AsNoTracking().AsQueryable();
            var f = Filter.Trim();
            if (!string.IsNullOrEmpty(f))
                q = q.Where(c => EF.Functions.Like(c.Nom, $"%{f}%"));

            var total = await q.CountAsync(cancellationToken);
            if (generation != _loadGeneration)
                return;

            if (resetPagination)
                Pagination.CurrentPage = 1;

            var list = await q.OrderBy(c => c.Nom)
                .Skip(Pagination.Skip)
                .Take(Pagination.PageSize)
                .ToListAsync(cancellationToken);

            if (generation != _loadGeneration)
                return;

            var selId = Selected?.Id;
            Items.Clear();
            foreach (var item in list)
                Items.Add(item);
            if (selId is { } id)
                Selected = Items.FirstOrDefault(x => x.Id == id);
            Pagination.TotalCount = total;
        }
        finally
        {
            if (generation == _loadGeneration)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenNewAsync(CancellationToken cancellationToken) =>
        ShowEditDialogAsync(null, cancellationToken);

    [RelayCommand]
    private Task OpenSelectedAsync(CancellationToken cancellationToken)
    {
        if (Selected == null) return Task.CompletedTask;
        return ShowEditDialogAsync(Selected.Id, cancellationToken);
    }

    private async Task ShowEditDialogAsync(int? id, CancellationToken cancellationToken)
    {
        if (!_session.CanAccessCharges) return;

        string? initialNom = null;
        var initialActif = true;
        if (id is { } existingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.CategoriesCharges.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == existingId, cancellationToken);
            if (existing == null) return;
            initialNom = existing.Nom;
            initialActif = existing.Actif;
        }

        var dialogTitle = id == null
            ? _locale.T("CategorieCharge_NewTitle")
            : _locale.Tf("CategorieCharge_TitleFmt", initialNom ?? string.Empty);

        var result = await _dialog.ShowCategorieChargeEditAsync(
            dialogTitle,
            _locale.T("Wm_Nom"),
            _locale.T("Lbl_Actif"),
            _locale.T("Btn_Cancel"),
            _locale.T("Btn_Save"),
            initialNom,
            initialActif,
            cancellationToken);

        if (result == null) return;
        if (string.IsNullOrWhiteSpace(result.Nom))
        {
            await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("CategorieCharge_ErrNom"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (id == null)
            {
                db.CategoriesCharges.Add(new CategorieCharge
                {
                    Nom = result.Nom.Trim(),
                    Actif = result.Actif,
                    CreatedByUserId = _session.UserId
                });
            }
            else
            {
                var entity = await db.CategoriesCharges.FirstAsync(c => c.Id == id, cancellationToken);
                entity.Nom = result.Nom.Trim();
                entity.Actif = result.Actif;
            }

            await db.SaveChangesAsync(cancellationToken);
            await LoadItemsAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActifAsync(CancellationToken cancellationToken)
    {
        if (Selected == null) return;
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var c = await db.CategoriesCharges.FirstAsync(x => x.Id == Selected.Id, cancellationToken);
            c.Actif = !c.Actif;
            await db.SaveChangesAsync(cancellationToken);
            await LoadItemsAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
