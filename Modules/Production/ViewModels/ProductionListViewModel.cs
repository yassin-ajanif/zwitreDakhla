using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Modules.Production.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Production.ViewModels;

public partial class ProductionListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly ICurrentUserSession _session;
    private readonly IProductionStockService _productionStock;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    public ProductionListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        ILocaleService locale,
        ICurrentUserSession session,
        IProductionStockService productionStock)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _locale = locale;
        _session = session;
        _productionStock = productionStock;
        _dateFrom = DateTime.Today;
        _dateTo = DateTime.Today;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        Title = _locale.T("Prod_ListTitle");
        _ = LoadAsync(CancellationToken.None);
    }

    public ObservableCollection<ProductionOperation> Operations { get; } = [];

    [ObservableProperty] private ProductionOperation? _selected;
    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _colTables = string.Empty;
    [ObservableProperty] private string _hdrVendre = string.Empty;
    [ObservableProperty] private string _hdrRetourner = string.Empty;
    [ObservableProperty] private string _colGrand = string.Empty;
    [ObservableProperty] private string _colMoyenne = string.Empty;
    [ObservableProperty] private string _colPetit = string.Empty;
    [ObservableProperty] private string _colPochette = string.Empty;
    [ObservableProperty] private string _colTotal = string.Empty;
    [ObservableProperty] private string _lblTotalOperation = string.Empty;

    private void RefreshUi()
    {
        BtnNew = _locale.T("Prod_BtnNew");
        UpdateBtnFilterDateText();
        MenuDelete = _locale.T("Prod_MenuDelete");
        ColTables = _locale.T("Prod_ColTables");
        HdrVendre = _locale.T("Prod_HdrVendre");
        HdrRetourner = _locale.T("Prod_HdrRetourner");
        ColGrand = _locale.T("Prod_ColGrand");
        ColMoyenne = _locale.T("Prod_ColMoyenne");
        ColPetit = _locale.T("Prod_ColPetit");
        ColPochette = _locale.T("Prod_ColPochette");
        ColTotal = _locale.T("Prod_ColTotal");
        LblTotalOperation = _locale.T("Prod_LblTotalOperation");
        Title = _locale.T("Prod_ListTitle");
        RefreshModifiedLabels();
    }

    private void RefreshModifiedLabels()
    {
        foreach (var op in Operations)
        {
            if (!op.WasModified) continue;
            var dt = op.UpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            op.ModifiedAtLabel = _locale.Tf("Prod_LblModifiedFmt", dt);
        }
    }

    private ProductionOperation MapOperation(OperationProduction entity)
    {
        var op = ProductionOperation.FromEntity(entity);
        if (op.WasModified)
        {
            var dt = entity.UpdatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            op.ModifiedAtLabel = _locale.Tf("Prod_LblModifiedFmt", dt);
        }
        return op;
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
            Operations.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var q = db.OperationsProduction.AsNoTracking().AsQueryable();

            if (_dateFrom.HasValue)
                q = q.Where(o => o.OperationAt.Date >= _dateFrom.Value.Date);
            if (_dateTo.HasValue)
                q = q.Where(o => o.OperationAt.Date <= _dateTo.Value.Date);

            var rows = await q
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync(cancellationToken);

            Operations.Clear();
            foreach (var row in rows)
                Operations.Add(MapOperation(row));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewOperationAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;
        await ShowEditDialogAsync(null, cancellationToken);
    }

    [RelayCommand]
    private async Task EditSelectedAsync(CancellationToken cancellationToken)
    {
        if (Selected == null) return;
        await ShowEditDialogAsync(Selected, cancellationToken);
    }

    [RelayCommand]
    private async Task EditOperationAsync(ProductionOperation? operation, CancellationToken cancellationToken)
    {
        if (operation == null) return;
        await ShowEditDialogAsync(operation, cancellationToken);
    }

    [RelayCommand]
    private async Task DeleteOperationAsync(ProductionOperation? operation, CancellationToken cancellationToken)
    {
        if (operation == null || !_session.CanAccessProduction) return;

        var ok = await _dialog.ConfirmAsync(
            _locale.T("Prod_ListTitle"),
            _locale.Tf("Prod_ConfirmDelete", operation.OperationTitle),
            cancellationToken);
        if (!ok) return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.OperationsProduction.FirstOrDefaultAsync(o => o.Id == operation.Id, cancellationToken);
        if (entity == null) return;

        await _productionStock.RemoveOperationStockAsync(
            db,
            entity.Id,
            entity.OperationAt,
            _session.UserId,
            cancellationToken);

        db.OperationsProduction.Remove(entity);
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

    private async Task ShowEditDialogAsync(ProductionOperation? existing, CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;

        var title = existing == null
            ? _locale.T("Prod_NewTitle")
            : _locale.Tf("Prod_EditTitleFmt", existing.OperationTitle);

        var result = await _dialog.ShowProductionOperationEditAsync(
            title,
            _locale.T("Prod_ColTables"),
            _locale.T("Prod_LblGrandPochets"),
            _locale.T("Prod_LblMoyennePochets"),
            _locale.T("Prod_LblPetitPochets"),
            _locale.T("Prod_LblTotalPreview"),
            _locale.T("Btn_Cancel"),
            _locale.T("Btn_Save"),
            existing?.Tables ?? 0,
            existing?.PochetteGrand ?? 0,
            existing?.PochetteMoyenne ?? 0,
            existing?.PochettePetit ?? 0,
            cancellationToken);

        if (result == null) return;

        var savedAt = DateTime.Now;
        var vm = new ProductionOperation
        {
            Tables = result.Tables,
            PochetteGrand = result.PochetteGrand,
            PochetteMoyenne = result.PochetteMoyenne,
            PochettePetit = result.PochettePetit
        };

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        int savedId;
        OperationProduction entity;

        if (existing == null)
        {
            entity = new OperationProduction();
            vm.ApplyTo(entity, savedAt);
            db.OperationsProduction.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            savedId = entity.Id;
        }
        else
        {
            entity = await db.OperationsProduction.FirstAsync(o => o.Id == existing.Id, cancellationToken);
            vm.ApplyTo(entity);
            savedId = entity.Id;
        }

        await _productionStock.SyncOperationStockAsync(
            db,
            savedId,
            vm.PochetteGrand,
            entity.OperationAt,
            _session.UserId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await LoadAsync(cancellationToken);
        Selected = Operations.FirstOrDefault(o => o.Id == savedId);
    }
}
