using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Production.ViewModels;
using GestionCommerciale.Modules.Charges.ViewModels;
using GestionCommerciale.Modules.AvoirFournisseur.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Devis.ViewModels;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.FactureFournisseur.ViewModels;
using GestionCommerciale.Modules.Livraison.ViewModels;
using GestionCommerciale.Modules.CommandeFournisseur.ViewModels;
using GestionCommerciale.Modules.CommandeClient.ViewModels;
using GestionCommerciale.Modules.Pos.ViewModels;
using GestionCommerciale.Modules.Reception.ViewModels;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Modules.Tiers.ViewModels;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace GestionCommerciale.Modules.Auth.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly PerformanceTestService _testService;
    private readonly IAppUpdateService _updateService;
    private readonly IDialogService _dialog;

    private UpdateInfo? _pendingUpdate;
    private VelopackAsset? _pendingRestartAsset;

    public AppShellViewModel(
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        PerformanceTestService testService,
        IAppUpdateService updateService,
        IDialogService dialog)
    {
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _testService = testService;
        _updateService = updateService;
        _dialog = dialog;
        UserLabel = session.Nom ?? string.Empty;
        _workspace.CurrentPageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WorkspaceCurrentPage));
            UpdateActiveNav();
        };
        _locale.CultureApplied += (_, _) => RefreshShellLabels();
        RefreshShellLabels();
        _workspace.Open(_sp.GetRequiredService<HomeViewModel>());
        UpdateActiveNav();
        _ = RunStartupUpdateCheckAsync();
    }

    private async Task RunStartupUpdateCheckAsync()
    {
        try
        {
            if (!_updateService.IsUpdateSupported)
                return;

            var pendingRestart = _updateService.PendingRestart;
            if (pendingRestart is not null)
            {
                _pendingRestartAsset = pendingRestart;
                AvailableUpdateVersion = pendingRestart.Version.ToString();
                IsUpdateAvailable = true;
                RefreshUpdateButtonLabel();
                return;
            }

            var result = await _updateService.CheckForUpdatesAsync();
            if (result.Status != AppUpdateStatus.UpdateAvailable || result.VelopackUpdate is null)
                return;

            _pendingUpdate = result.VelopackUpdate;
            AvailableUpdateVersion = result.Update?.Version ?? "?";
            IsUpdateAvailable = true;
            RefreshUpdateButtonLabel();
        }
        catch
        {
            // Ignore update check failures on startup.
        }
    }

    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private bool _isUpdateInstalling;
    [ObservableProperty] private string _updateButtonLabel = string.Empty;
    [ObservableProperty] private string _availableUpdateVersion = string.Empty;

    private void RefreshUpdateButtonLabel()
    {
        UpdateButtonLabel = IsUpdateInstalling
            ? _locale.T("Update_Installing")
            : string.Format(_locale.T("Update_InstallBtn"), AvailableUpdateVersion);
    }

    [RelayCommand]
    private async Task InstallUpdateAsync(CancellationToken cancellationToken)
    {
        if (IsUpdateInstalling)
            return;

        IsUpdateInstalling = true;
        RefreshUpdateButtonLabel();

        try
        {
            if (_pendingRestartAsset is not null)
            {
                _updateService.ApplyPendingRestart(_pendingRestartAsset);
                return;
            }

            if (_pendingUpdate is not null)
                await _updateService.DownloadAndApplyUpdatesAsync(_pendingUpdate, progress: null, cancellationToken);
        }
        catch (Exception ex)
        {
            IsUpdateInstalling = false;
            RefreshUpdateButtonLabel();
            await _dialog.ShowExceptionAsync(
                _locale.T("Update_Title"),
                ex,
                string.Format(_locale.T("Update_DownloadFailed"), ex.Message),
                cancellationToken);
        }
    }

    public BaseViewModel? WorkspaceCurrentPage => _workspace.CurrentPage;

    [ObservableProperty] private string _userLabel = string.Empty;

    [ObservableProperty] private string _navHome = string.Empty;
    [ObservableProperty] private string _navPos = string.Empty;
    [ObservableProperty] private string _navVente = string.Empty;
    [ObservableProperty] private string _navAchat = string.Empty;
    [ObservableProperty] private string _navClients = string.Empty;
    [ObservableProperty] private string _navDevis = string.Empty;
    [ObservableProperty] private string _navBcc = string.Empty;
    [ObservableProperty] private string _navBl = string.Empty;
    [ObservableProperty] private string _navFactures = string.Empty;
    [ObservableProperty] private string _navAvoirs = string.Empty;
    [ObservableProperty] private string _navAvoirFournisseur = string.Empty;
    [ObservableProperty] private string _navFournisseurs = string.Empty;
    [ObservableProperty] private string _navBc = string.Empty;
    [ObservableProperty] private string _navBr = string.Empty;
    [ObservableProperty] private string _navFacturesFournisseur = string.Empty;
    [ObservableProperty] private string _navStockAdmin = string.Empty;
    [ObservableProperty] private string _navStock = string.Empty;
    [ObservableProperty] private string _navProduits = string.Empty;
    [ObservableProperty] private string _navReports = string.Empty;
    [ObservableProperty] private string _navSettings = string.Empty;
    [ObservableProperty] private string _navCharges = string.Empty;
    [ObservableProperty] private string _navProduction = string.Empty;

    [ObservableProperty] private bool _isTestRunning;
    [ObservableProperty] private string _testProgress = string.Empty;

    [ObservableProperty] private bool _isNavHomeActive;
    [ObservableProperty] private bool _isNavPosActive;
    [ObservableProperty] private bool _isNavClientsActive;
    [ObservableProperty] private bool _isNavFournisseursActive;
    [ObservableProperty] private bool _isNavDevisActive;
    [ObservableProperty] private bool _isNavBccActive;
    [ObservableProperty] private bool _isNavBlActive;
    [ObservableProperty] private bool _isNavFacturesActive;
    [ObservableProperty] private bool _isNavAvoirsActive;
    [ObservableProperty] private bool _isNavAvoirFournisseurActive;
    [ObservableProperty] private bool _isNavBcActive;
    [ObservableProperty] private bool _isNavBrActive;
    [ObservableProperty] private bool _isNavFacturesFournisseurActive;
    [ObservableProperty] private bool _isNavStockActive;
    [ObservableProperty] private bool _isNavProduitsActive;
    [ObservableProperty] private bool _isNavReportsActive;
    [ObservableProperty] private bool _isNavSettingsActive;
    [ObservableProperty] private bool _isNavChargesActive;
    [ObservableProperty] private bool _isNavProductionActive;

    private void RefreshShellLabels()
    {
        NavHome = _locale.T("Nav_Home");
        NavPos = _locale.T("Nav_Pos");
        NavVente = _locale.T("Nav_Vente");
        NavAchat = _locale.T("Nav_Achat");
        NavClients = _locale.T("Nav_Clients");
        NavDevis = _locale.T("Nav_Devis");
        NavBcc = _locale.T("Nav_BCC");
        NavBl = _locale.T("Nav_BL");
        NavFactures = _locale.T("Nav_Factures");
        NavAvoirs = _locale.T("Nav_Avoirs");
        NavAvoirFournisseur = _locale.T("Nav_AvoirFournisseur");
        NavFournisseurs = _locale.T("Nav_Fournisseurs");
        NavBc = _locale.T("Nav_BC");
        NavBr = _locale.T("Nav_BR");
        NavFacturesFournisseur = _locale.T("Nav_FacturesFournisseur");
        NavStockAdmin = _locale.T("Nav_StockAdmin");
        NavStock = _locale.T("Nav_Stock");
        NavProduits = _locale.T("Nav_Produits");
        NavReports = _locale.T("Nav_Reports");
        NavSettings = _locale.T("Nav_Settings");
        NavCharges = _locale.T("Nav_Charges");
        NavProduction = _locale.T("Nav_Production");
        RefreshUpdateButtonLabel();
        Title = NavHome;
    }

    [ObservableProperty] private bool _venteNavExpanded = true;
    [ObservableProperty] private bool _achatNavExpanded = true;
    [ObservableProperty] private bool _footerNavExpanded = true;
    public string VenteNavArrow => VenteNavExpanded ? "\u25BC" : "\u25B6";
    public string AchatNavArrow => AchatNavExpanded ? "\u25BC" : "\u25B6";
    public string FooterNavArrow => FooterNavExpanded ? "\u25BC" : "\u25B6";

    partial void OnVenteNavExpandedChanged(bool value) => OnPropertyChanged(nameof(VenteNavArrow));
    partial void OnAchatNavExpandedChanged(bool value) => OnPropertyChanged(nameof(AchatNavArrow));
    partial void OnFooterNavExpandedChanged(bool value) => OnPropertyChanged(nameof(FooterNavArrow));

    [RelayCommand]
    private void ToggleVenteNav() => VenteNavExpanded = !VenteNavExpanded;

    [RelayCommand]
    private void ToggleAchatNav() => AchatNavExpanded = !AchatNavExpanded;

    [RelayCommand]
    private void ToggleFooterNav() => FooterNavExpanded = !FooterNavExpanded;

    public bool ShowNavClients => _session.CanAccessClients;
    public bool ShowNavFournisseurs => _session.CanAccessFournisseurs;
    public bool ShowNavStock => _session.CanAccessStock;
    public bool ShowNavProduits => _session.CanAccessStock;
    public bool ShowNavDevis => _session.CanAccessDevis;
    public bool ShowNavBCC => _session.CanAccessDevis;
    public bool ShowNavBL => _session.CanAccessBL;
    public bool ShowNavBR => _session.CanAccessBR;
    public bool ShowNavBC => _session.CanAccessBC;
    public bool ShowNavFactures => _session.CanAccessFacturation;
    public bool ShowNavAvoirs => _session.CanAccessAvoir;
    public bool ShowNavFacturesFournisseur => _session.CanAccessFacturation;
    public bool ShowNavAvoirFournisseur => _session.CanAccessAvoir;
    public bool ShowNavReports => _session.CanAccessReporting;
    public bool ShowNavSettings => _session.CanAccessSettings;
    public bool ShowNavCharges => _session.CanAccessCharges;
    public bool ShowNavProduction => _session.CanAccessProduction;

    [RelayCommand]
    private void GoHome() => _workspace.Open(_sp.GetRequiredService<HomeViewModel>());

    [RelayCommand]
    private void GoPos() => _workspace.Open(_sp.GetRequiredService<PosViewModel>());

    [RelayCommand]
    private void GoClients()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Clients);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoFournisseurs()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Fournisseurs);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoStock() => _workspace.Open(_sp.GetRequiredService<StockMainViewModel>());

    [RelayCommand]
    private void GoProduits() => _workspace.Open(_sp.GetRequiredService<ProduitsViewModel>());

    [RelayCommand]
    private void GoReports()
    {
        var vm = _sp.GetRequiredService<ReportsListViewModel>();
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoDevis() => _workspace.Open(_sp.GetRequiredService<DevisListViewModel>());

    [RelayCommand]
    private void GoBCV()
    {
        var vm = _sp.GetRequiredService<BCVListViewModel>();
        _workspace.Open(vm);
        vm.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void GoBL() => _workspace.Open(_sp.GetRequiredService<BLListViewModel>());

    [RelayCommand]
    private void GoBR() => _workspace.Open(_sp.GetRequiredService<BRListViewModel>());

    [RelayCommand]
    private void GoBC()
    {
        var vm = _sp.GetRequiredService<BCListViewModel>();
        _workspace.Open(vm);
        vm.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void GoFactures() => _workspace.Open(_sp.GetRequiredService<FactureListViewModel>());

    [RelayCommand]
    private void GoAvoirs() => _workspace.Open(_sp.GetRequiredService<AvoirListViewModel>());

    [RelayCommand]
    private void GoFacturesFournisseur() => _workspace.Open(_sp.GetRequiredService<FactureFournisseurListViewModel>());

    [RelayCommand]
    private void GoAvoirFournisseur() => _workspace.Open(_sp.GetRequiredService<AvoirFournisseurListViewModel>());

    [RelayCommand]
    private void GoSettings() => _workspace.Open(_sp.GetRequiredService<SettingsViewModel>());

    [RelayCommand]
    private void GoCharges() => _workspace.Open(_sp.GetRequiredService<ChargeListViewModel>());

    [RelayCommand]
    private void GoProduction() => _workspace.Open(_sp.GetRequiredService<ProductionListViewModel>());

    [RelayCommand]
    private async Task RunPerfTestAsync(CancellationToken ct)
    {
        if (IsTestRunning) return;
        IsTestRunning = true;
        TestProgress = string.Empty;
        try
        {
            var progress = new Progress<string>(msg => TestProgress = msg);
            var result = await _testService.RunAsync(progress, ct);
            TestProgress = result;
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    private void UpdateActiveNav()
    {
        var p = _workspace.CurrentPage;
        IsNavHomeActive = p is HomeViewModel;
        IsNavPosActive = p is PosViewModel;
        IsNavClientsActive = p is TiersListViewModel tl && tl.Scope == TiersListScope.Clients
            || p is TiersDetailViewModel td && td.ListScope == TiersListScope.Clients;
        IsNavFournisseursActive = p is TiersListViewModel tiersList && tiersList.Scope == TiersListScope.Fournisseurs
            || p is TiersDetailViewModel tiersDetail && tiersDetail.ListScope == TiersListScope.Fournisseurs;
        IsNavDevisActive = p is DevisListViewModel or DevisEditViewModel;
        IsNavBccActive = p is BCVListViewModel or BCVEditViewModel;
        IsNavBlActive = p is BLListViewModel or BLEditViewModel;
        IsNavFacturesActive = p is FactureListViewModel or FactureEditViewModel;
        IsNavAvoirsActive = p is AvoirListViewModel or AvoirEditViewModel;
        IsNavAvoirFournisseurActive = p is AvoirFournisseurListViewModel or AvoirFournisseurEditViewModel;
        IsNavBcActive = p is BCListViewModel or BCEditViewModel;
        IsNavBrActive = p is BRListViewModel or BREditViewModel;
        IsNavFacturesFournisseurActive = p is FactureFournisseurListViewModel or FactureFournisseurEditViewModel;
        IsNavStockActive = p is StockMainViewModel;
        IsNavProduitsActive = p is ProduitsViewModel;
        IsNavReportsActive = p is ReportsListViewModel;
        IsNavSettingsActive = p is SettingsViewModel;
        IsNavChargesActive = p is ChargeListViewModel or ChargeEditViewModel;
        IsNavProductionActive = p is ProductionListViewModel or CommandeProductionEditViewModel;
    }
}
