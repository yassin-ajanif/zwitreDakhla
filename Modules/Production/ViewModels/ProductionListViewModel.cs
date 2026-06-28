using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.Modules.Production.ViewModels;

public partial class ProductionListViewModel : BaseViewModel
{
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly ICurrentUserSession _session;

    private readonly List<ProductionOperation> _allOperations = [];
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int _nextId = 1;

    public ProductionListViewModel(
        IDialogService dialog,
        ILocaleService locale,
        ICurrentUserSession session)
    {
        _dialog = dialog;
        _locale = locale;
        _session = session;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        SeedMockData();
        ApplyFilter();
        Title = _locale.T("Prod_ListTitle");
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
    }

    private void UpdateBtnFilterDateText()
    {
        BtnFilterDate = _dateFrom is { } from && _dateTo is { } to
            ? $"{from:dd/MM/yy} — {to:dd/MM/yy}"
            : _locale.T("Btn_FilterDate");
    }

    private void SeedMockData()
    {
        _allOperations.Clear();
        _allOperations.Add(CreateOperation(new DateTime(2026, 6, 28, 9, 0, 0), 64, 512, 100, 50));
        _allOperations.Add(CreateOperation(new DateTime(2026, 6, 28, 17, 30, 0), 58, 400, 80, 30));
        _allOperations.Add(CreateOperation(new DateTime(2026, 6, 27, 10, 0, 0), 60, 480, 90, 40));
    }

    private ProductionOperation CreateOperation(DateTime date, int tables, int grand, int moyenne, int petit) =>
        new()
        {
            Id = _nextId++,
            Date = date.Date,
            CreatedAt = date,
            Tables = tables,
            PochetteGrand = grand,
            PochetteMoyenne = moyenne,
            PochettePetit = petit
        };

    private void ApplyFilter()
    {
        Operations.Clear();
        foreach (var op in _allOperations
                     .Where(o => (!_dateFrom.HasValue || o.Date >= _dateFrom.Value.Date)
                              && (!_dateTo.HasValue || o.Date <= _dateTo.Value.Date))
                     .OrderByDescending(o => o.CreatedAt))
            Operations.Add(op);
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

        _allOperations.RemoveAll(o => o.Id == operation.Id);
        ApplyFilter();
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null) return;
        _dateFrom = range.Value.from.Date;
        _dateTo = range.Value.to.Date;
        UpdateBtnFilterDateText();
        ApplyFilter();
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

        if (existing == null)
        {
            _allOperations.Add(new ProductionOperation
            {
                Id = _nextId++,
                Date = savedAt.Date,
                CreatedAt = savedAt,
                Tables = result.Tables,
                PochetteGrand = result.PochetteGrand,
                PochetteMoyenne = result.PochetteMoyenne,
                PochettePetit = result.PochettePetit
            });
        }
        else
        {
            var idx = _allOperations.FindIndex(o => o.Id == existing.Id);
            if (idx >= 0)
            {
                _allOperations[idx] = new ProductionOperation
                {
                    Id = existing.Id,
                    Date = savedAt.Date,
                    CreatedAt = savedAt,
                    Tables = result.Tables,
                    PochetteGrand = result.PochetteGrand,
                    PochetteMoyenne = result.PochetteMoyenne,
                    PochettePetit = result.PochettePetit
                };
            }
        }

        var savedId = existing?.Id ?? _allOperations[^1].Id;
        ApplyFilter();
        Selected = Operations.FirstOrDefault(o => o.Id == savedId);
    }
}
