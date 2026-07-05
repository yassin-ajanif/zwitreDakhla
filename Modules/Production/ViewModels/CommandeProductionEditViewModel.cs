using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Production.Models;
using GestionCommerciale.Modules.Production.Services;
using GestionCommerciale.Modules.Reception.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiersEntity = global::GestionCommerciale.Modules.Tiers.Models.Tiers;
using TypeTiers = global::GestionCommerciale.Modules.Tiers.Models.TypeTiers;

namespace GestionCommerciale.Modules.Production.ViewModels;

public partial class CommandeProductionEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IProductionStockService _productionStock;
    private readonly ICommandeProductionReceptionService _commandeReception;
    private bool _suppressEstTermineeExpirationSync;
    private int? _pendingHighlightOperationId;

    public event Action<int>? ScrollToOperationRequested;

    public CommandeProductionEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IProductionStockService productionStock,
        ICommandeProductionReceptionService commandeReception)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _productionStock = productionStock;
        _commandeReception = commandeReception;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        Title = _locale.T("CmdProd_Title");
    }

    public ObservableCollection<TiersEntity> Fournisseurs { get; } = [];
    public ObservableCollection<TypeHuitre> TypesHuitre { get; } = [];
    public ObservableCollection<TypeHuitre> AllTypesHuitre { get; } = [];
    public ObservableCollection<CategorieCommande> CategoriesCommande { get; } = [];
    public ObservableCollection<CategorieCommande> AllCategoriesCommande { get; } = [];
    public ObservableCollection<ProductionOperation> Operations { get; } = [];

    [ObservableProperty] private ProductionOperation? _selectedOperation;
    [ObservableProperty] private TypeHuitre? _selectedPanelTypeHuitre;
    [ObservableProperty] private CategorieCommande? _selectedPanelCategorieCommande;

    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;

    [ObservableProperty] private int? _commandeId;
    [ObservableProperty] private int? _linkedBonReceptionId;
    [ObservableProperty] private string _linkedBonReceptionLabel = string.Empty;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private TiersEntity? _selectedFournisseur;
    [ObservableProperty] private TypeHuitre? _selectedTypeHuitre;
    [ObservableProperty] private CategorieCommande? _selectedCategorieCommande;
    [ObservableProperty] private int _quantiteNaissain;
    [ObservableProperty] private decimal _prixAchatNaissainHt;
    [ObservableProperty] private bool _estTerminee;
    [ObservableProperty] private DateTimeOffset _dateCommande = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _dateExpiration;
    [ObservableProperty] private string _note = string.Empty;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnAddOperation = string.Empty;
    [ObservableProperty] private string _menuDeleteOperation = string.Empty;
    [ObservableProperty] private string _lblFournisseur = string.Empty;
    [ObservableProperty] private string _wmFournisseurSearch = string.Empty;
    [ObservableProperty] private string _lblTypeHuitre = string.Empty;
    [ObservableProperty] private string _lblCategorieCommande = string.Empty;
    [ObservableProperty] private string _lblQuantiteNaissain = string.Empty;
    [ObservableProperty] private string _lblPrixAchatNaissain = string.Empty;
    [ObservableProperty] private string _lblTauxMortalite = string.Empty;
    [ObservableProperty] private string _lblTauxAgrandissement = string.Empty;
    [ObservableProperty] private string _lblDateCommande = string.Empty;
    [ObservableProperty] private string _lblDateExpiration = string.Empty;
    [ObservableProperty] private string _lblEtat = string.Empty;
    [ObservableProperty] private string _lblEnCours = string.Empty;
    [ObservableProperty] private string _lblTerminee = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _lblRemainingHuitresChipPrefix = string.Empty;
    [ObservableProperty] private string _sectionOperations = string.Empty;
    [ObservableProperty] private string _lblTotalCommande = string.Empty;
    [ObservableProperty] private string _lblTotauxRow = string.Empty;
    [ObservableProperty] private string _hdrVendre = string.Empty;
    [ObservableProperty] private string _hdrRetourner = string.Empty;
    [ObservableProperty] private string _colGrand = string.Empty;
    [ObservableProperty] private string _colMoyenne = string.Empty;
    [ObservableProperty] private string _colPetit = string.Empty;
    [ObservableProperty] private string _colPochette = string.Empty;
    [ObservableProperty] private string _colTotal = string.Empty;
    [ObservableProperty] private string _colTables = string.Empty;
    [ObservableProperty] private string _lblTotalOperation = string.Empty;

    [ObservableProperty] private string _lblTypesHuitrePanel = string.Empty;
    [ObservableProperty] private string _wmNewTypeHuitre = string.Empty;
    [ObservableProperty] private string _btnAddPanelTypeHuitre = string.Empty;
    [ObservableProperty] private string _menuDeleteTypeHuitre = string.Empty;
    [ObservableProperty] private string _newTypeHuitreNom = string.Empty;

    [ObservableProperty] private string _lblCategoriesCommandePanel = string.Empty;
    [ObservableProperty] private string _wmNewCategorieCommande = string.Empty;
    [ObservableProperty] private string _btnAddPanelCategorieCommande = string.Empty;
    [ObservableProperty] private string _menuDeleteCategorieCommande = string.Empty;
    [ObservableProperty] private string _newCategorieCommandeNom = string.Empty;

    [ObservableProperty] private string _colLookupNom = string.Empty;
    [ObservableProperty] private string _colLookupActif = string.Empty;

    public string TotalCommandeLabel => Operations.Sum(o => o.TotalOperation).ToString("N0", CultureInfo.CurrentCulture);
    public bool CanAddOperation => CommandeId != null;
    public bool HasLinkedBonReceptionLabel => LinkedBonReceptionId is > 0;
    public bool HasOperations => Operations.Count > 0;

    public string TotalTablesLabel => Operations.Sum(o => o.Tables).ToString("N0", CultureInfo.CurrentCulture);
    public string TotalPochetteGrandLabel => Operations.Sum(o => o.PochetteGrand).ToString("N0", CultureInfo.CurrentCulture);
    public string TotalGrandHuitresLabel => Operations.Sum(o => o.TotalGrand).ToString("N0", CultureInfo.CurrentCulture);
    public string TotalTablesGrandLabel => ProductionOperation.FormatTablesLabel(
        ProductionOperation.ComputeTablesFromPochettes(Operations.Sum(o => o.PochetteGrand)));
    public string TotalPochetteMoyenneLabel => Operations.Sum(o => o.PochetteMoyenne).ToString("N0", CultureInfo.CurrentCulture);
    public string TotalTablesMoyenneLabel => ProductionOperation.FormatTablesLabel(
        ProductionOperation.ComputeTablesFromPochettes(Operations.Sum(o => o.PochetteMoyenne)));
    public string TotalPochettePetitLabel => Operations.Sum(o => o.PochettePetit).ToString("N0", CultureInfo.CurrentCulture);
    public string TotalTablesPetitLabel => ProductionOperation.FormatTablesLabel(
        ProductionOperation.ComputeTablesFromPochettes(Operations.Sum(o => o.PochettePetit)));

    public int SumGrandHuitres => ProductionOperation.SumGrandHuitres(Operations);

    public decimal TauxMortalite =>
        ProductionOperation.ComputeTauxMortalitePercent(QuantiteNaissain, SumGrandHuitres);

    public string TauxMortaliteLabel =>
        ProductionOperation.FormatTauxMortaliteLabel(TauxMortalite);

    public int? DureeAgrandissementJours =>
        EstTerminee
            ? ProductionOperation.ComputeDureeAgrandissementJours(DateCommande.DateTime, DateExpiration?.DateTime)
            : null;

    public string TauxAgrandissementLabel =>
        ProductionOperation.FormatTauxAgrandissementLabel(DureeAgrandissementJours);

    public bool ShowTauxMortalite => EstTerminee;
    public bool ShowTauxAgrandissement => DureeAgrandissementJours.HasValue;

    public bool ShowRemainingHuitresChip => QuantiteNaissain > 0;

    public string RemainingHuitresAtWaterLabel =>
        ProductionOperation.ComputeRemainingHuitresAtWater(QuantiteNaissain, SumGrandHuitres)
            .ToString("N0", CultureInfo.CurrentCulture);

    partial void OnCommandeIdChanged(int? value) => OnPropertyChanged(nameof(CanAddOperation));

    partial void OnLinkedBonReceptionIdChanged(int? value) => OnPropertyChanged(nameof(HasLinkedBonReceptionLabel));

    partial void OnEstTermineeChanged(bool value)
    {
        if (!_suppressEstTermineeExpirationSync)
            DateExpiration = value ? DateTimeOffset.Now : null;

        OnPropertyChanged(nameof(ShowTauxMortalite));
        RefreshTauxMortalite();
        RefreshTauxAgrandissement();
    }

    partial void OnDateCommandeChanged(DateTimeOffset value) => RefreshTauxAgrandissement();

    partial void OnDateExpirationChanged(DateTimeOffset? value) => RefreshTauxAgrandissement();

    partial void OnQuantiteNaissainChanged(int value)
    {
        RefreshTauxMortalite();
        RefreshRemainingHuitresChip();
    }

    private void RefreshRemainingHuitresChip()
    {
        OnPropertyChanged(nameof(ShowRemainingHuitresChip));
        OnPropertyChanged(nameof(RemainingHuitresAtWaterLabel));
    }

    private void RefreshTauxMortalite()
    {
        OnPropertyChanged(nameof(SumGrandHuitres));
        OnPropertyChanged(nameof(TauxMortalite));
        OnPropertyChanged(nameof(TauxMortaliteLabel));
    }

    private void RefreshTauxAgrandissement()
    {
        OnPropertyChanged(nameof(DureeAgrandissementJours));
        OnPropertyChanged(nameof(TauxAgrandissementLabel));
        OnPropertyChanged(nameof(ShowTauxAgrandissement));
    }

    private void RefreshOperationTotals()
    {
        OnPropertyChanged(nameof(HasOperations));
        OnPropertyChanged(nameof(TotalTablesLabel));
        OnPropertyChanged(nameof(TotalPochetteGrandLabel));
        OnPropertyChanged(nameof(TotalGrandHuitresLabel));
        OnPropertyChanged(nameof(TotalTablesGrandLabel));
        OnPropertyChanged(nameof(TotalPochetteMoyenneLabel));
        OnPropertyChanged(nameof(TotalTablesMoyenneLabel));
        OnPropertyChanged(nameof(TotalPochettePetitLabel));
        OnPropertyChanged(nameof(TotalTablesPetitLabel));
        OnPropertyChanged(nameof(TotalCommandeLabel));
        RefreshTauxMortalite();
        RefreshRemainingHuitresChip();
    }

    public void LoadNew()
    {
        CommandeId = null;
        Numero = string.Empty;
        SelectedFournisseur = null;
        SelectedTypeHuitre = null;
        SelectedCategorieCommande = null;
        QuantiteNaissain = 0;
        PrixAchatNaissainHt = 0;
        EstTerminee = false;
        DateCommande = DateTimeOffset.Now;
        DateExpiration = null;
        Note = string.Empty;
        SetLinkedBonReception(null, null);
        Operations.Clear();
        ClearOperationHighlights();
        Title = _locale.T("CmdProd_NewTitle");
        RefreshOperationTotals();
        _ = LoadLookupsAsync(CancellationToken.None);
    }

    public void Load(int commandeId) => Load(commandeId, null);

    public void Load(int commandeId, int? highlightOperationId)
    {
        _pendingHighlightOperationId = highlightOperationId;
        CommandeId = commandeId;
        _ = LoadAsync(commandeId, CancellationToken.None);
    }

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnAddOperation = _locale.T("CmdProd_BtnAddOperation");
        MenuDeleteOperation = _locale.T("Prod_MenuDelete");
        LblFournisseur = _locale.T("Avf_LblFournisseur");
        WmFournisseurSearch = _locale.T("Wm_SearchSupplier");
        LblTypeHuitre = _locale.T("CmdProd_LblTypeHuitre");
        LblCategorieCommande = _locale.T("CmdProd_LblCategorieCommande");
        LblQuantiteNaissain = _locale.T("CmdProd_LblQuantiteNaissain");
        LblPrixAchatNaissain = _locale.T("CmdProd_LblPrixAchatNaissain");
        LblTauxMortalite = _locale.T("CmdProd_LblTauxMortalite");
        LblTauxAgrandissement = _locale.T("CmdProd_LblTauxAgrandissement");
        LblDateCommande = _locale.T("CmdProd_LblDate");
        LblDateExpiration = _locale.T("CmdProd_LblDateExpiration");
        LblEtat = _locale.T("CmdProd_LblEtat");
        LblEnCours = _locale.T("CmdProd_EnCours");
        LblTerminee = _locale.T("CmdProd_Terminee");
        LblNote = _locale.T("Lbl_Note");
        LblRemainingHuitresChipPrefix = _locale.T("CmdProd_RemainingHuitresChipPrefix");
        SectionOperations = _locale.T("CmdProd_SectionOperations");
        LblTotalCommande = _locale.T("CmdProd_LblTotalCommande");
        LblTotauxRow = _locale.T("CmdProd_LblTotauxRow");
        HdrVendre = _locale.T("Prod_HdrVendre");
        HdrRetourner = _locale.T("Prod_HdrRetourner");
        ColGrand = _locale.T("Prod_ColGrand");
        ColMoyenne = _locale.T("Prod_ColMoyenne");
        ColPetit = _locale.T("Prod_ColPetit");
        ColPochette = _locale.T("Prod_ColPochette");
        ColTotal = _locale.T("Prod_ColTotal");
        ColTables = _locale.T("Prod_ColTables");
        LblTotalOperation = _locale.T("Prod_LblTotalOperation");
        LblTypesHuitrePanel = _locale.T("TypeHuitre_PanelTitle");
        WmNewTypeHuitre = _locale.T("TypeHuitre_WmNew");
        BtnAddPanelTypeHuitre = _locale.T("Chg_BtnAddCategory");
        MenuDeleteTypeHuitre = _locale.T("TypeHuitre_MenuDelete");
        LblCategoriesCommandePanel = _locale.T("CatCmd_PanelTitle");
        WmNewCategorieCommande = _locale.T("CatCmd_WmNew");
        BtnAddPanelCategorieCommande = _locale.T("Chg_BtnAddCategory");
        MenuDeleteCategorieCommande = _locale.T("CatCmd_MenuDelete");
        ColLookupNom = _locale.T("Lbl_ColNom");
        ColLookupActif = _locale.T("Lbl_ColActif");
        RefreshModifiedLabels();
        RefreshOperationTotals();
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

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await LoadLookupsAsync(db, cancellationToken, null, null);
        await LoadFournisseursAsync(db, cancellationToken);
    }

    private async Task LoadLookupsAsync(
        AppDbContext db,
        CancellationToken cancellationToken,
        int? selectTypeId,
        int? selectCategorieId)
    {
        TypesHuitre.Clear();
        AllTypesHuitre.Clear();
        var allTypes = await db.TypesHuitre.AsNoTracking()
            .OrderBy(t => t.Ordre)
            .ThenBy(t => t.Nom)
            .ToListAsync(cancellationToken);
        foreach (var t in allTypes)
            AllTypesHuitre.Add(t);
        foreach (var t in allTypes.Where(x => x.Actif))
            TypesHuitre.Add(t);

        var typeId = selectTypeId ?? SelectedTypeHuitre?.Id;
        if (typeId is > 0 && TypesHuitre.All(t => t.Id != typeId))
        {
            var currentType = allTypes.FirstOrDefault(t => t.Id == typeId);
            if (currentType != null)
                TypesHuitre.Insert(0, currentType);
        }

        SelectedTypeHuitre = typeId is > 0
            ? TypesHuitre.FirstOrDefault(t => t.Id == typeId)
            : TypesHuitre.FirstOrDefault();
        SelectedPanelTypeHuitre = AllTypesHuitre.FirstOrDefault(t => t.Id == SelectedTypeHuitre?.Id)
            ?? AllTypesHuitre.FirstOrDefault();

        CategoriesCommande.Clear();
        AllCategoriesCommande.Clear();
        var allCategories = await db.CategoriesCommande.AsNoTracking()
            .OrderBy(c => c.Ordre)
            .ThenBy(c => c.Nom)
            .ToListAsync(cancellationToken);
        foreach (var c in allCategories)
            AllCategoriesCommande.Add(c);
        foreach (var c in allCategories.Where(x => x.Actif))
            CategoriesCommande.Add(c);

        var categorieId = selectCategorieId ?? SelectedCategorieCommande?.Id;
        if (categorieId is > 0 && CategoriesCommande.All(c => c.Id != categorieId))
        {
            var currentCat = allCategories.FirstOrDefault(c => c.Id == categorieId);
            if (currentCat != null)
                CategoriesCommande.Insert(0, currentCat);
        }

        SelectedCategorieCommande = categorieId is > 0
            ? CategoriesCommande.FirstOrDefault(c => c.Id == categorieId)
            : CategoriesCommande.FirstOrDefault();
        SelectedPanelCategorieCommande = AllCategoriesCommande.FirstOrDefault(c => c.Id == SelectedCategorieCommande?.Id)
            ?? AllCategoriesCommande.FirstOrDefault();
    }

    private async Task LoadFournisseursAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Fournisseur || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom)
            .ToListAsync(cancellationToken);
        Fournisseurs.Clear();
        foreach (var row in rows)
            Fournisseurs.Add(row);
    }

    private async Task LoadAsync(int commandeId, CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.CommandesProduction
                .AsNoTracking()
                .Include(c => c.Operations)
                .FirstAsync(c => c.Id == commandeId, cancellationToken);

            await LoadLookupsAsync(db, cancellationToken, entity.TypeHuitreId, entity.CategorieCommandeId);
            await LoadFournisseursAsync(db, cancellationToken);

            Numero = entity.Numero;
            SelectedFournisseur = Fournisseurs.FirstOrDefault(f => f.Id == entity.FournisseurId);
            QuantiteNaissain = entity.QuantiteNaissain;
            PrixAchatNaissainHt = entity.PrixAchatNaissainHT;
            DateCommande = entity.DateCommande;
            Note = entity.Note;

            _suppressEstTermineeExpirationSync = true;
            EstTerminee = entity.EstTerminee;
            DateExpiration = entity.DateExpiration;
            _suppressEstTermineeExpirationSync = false;

            Operations.Clear();
            foreach (var op in entity.Operations.OrderByDescending(o => o.OperationAt))
                Operations.Add(MapOperation(op));

            ApplyOperationHighlight(_pendingHighlightOperationId);
            _pendingHighlightOperationId = null;

            Title = _locale.Tf("CmdProd_EditTitleFmt", Numero);
            await RefreshLinkedBonReceptionAsync(db, commandeId, cancellationToken);
            OnPropertyChanged(nameof(CanAddOperation));
            OnPropertyChanged(nameof(ShowTauxMortalite));
            RefreshTauxAgrandissement();
            RefreshOperationTotals();
        }
        finally
        {
            IsBusy = false;
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

    private void ApplyOperationHighlight(int? operationId)
    {
        ClearOperationHighlights();
        if (operationId is not int opId)
            return;

        var op = Operations.FirstOrDefault(o => o.Id == opId);
        if (op == null)
            return;

        op.IsHighlighted = true;
        SelectedOperation = op;
        ScrollToOperationRequested?.Invoke(opId);
    }

    private void ClearOperationHighlights()
    {
        foreach (var op in Operations)
            op.IsHighlighted = false;
    }

    [RelayCommand]
    private void Back() => _workspace.Open(_sp.GetRequiredService<ProductionListViewModel>());

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;

        if (SelectedFournisseur == null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrFournisseur"), cancellationToken);
            return;
        }

        if (SelectedTypeHuitre == null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrTypeHuitre"), cancellationToken);
            return;
        }

        if (SelectedCategorieCommande == null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrCategorieCommande"), cancellationToken);
            return;
        }

        if (QuantiteNaissain <= 0)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrQuantite"), cancellationToken);
            return;
        }

        if (PrixAchatNaissainHt <= 0)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrPrixAchat"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            CommandeProduction entity;

            if (CommandeId == null)
            {
                var num = await _numbers.NextCommandeProductionAsync(cancellationToken);
                entity = new CommandeProduction
                {
                    Numero = num,
                    CreatedByUserId = _session.UserId
                };
                db.CommandesProduction.Add(entity);
            }
            else
            {
                entity = await db.CommandesProduction.FirstAsync(c => c.Id == CommandeId.Value, cancellationToken);
            }

            entity.FournisseurId = SelectedFournisseur.Id;
            entity.TypeHuitreId = SelectedTypeHuitre.Id;
            entity.CategorieCommandeId = SelectedCategorieCommande.Id;
            entity.QuantiteNaissain = QuantiteNaissain;
            entity.PrixAchatNaissainHT = PrixAchatNaissainHt;
            entity.EstTerminee = EstTerminee;
            entity.TauxMortalite = EstTerminee ? TauxMortalite : 0;
            entity.DateCommande = DateCommande.DateTime;
            entity.DateExpiration = DateExpiration?.DateTime;
            entity.Note = Note.Trim();

            await db.SaveChangesAsync(cancellationToken);

            await _commandeReception.SyncBonReceptionAsync(db, entity, _session.UserId, cancellationToken);
            await RefreshLinkedBonReceptionAsync(db, entity.Id, cancellationToken);

            CommandeId = entity.Id;
            Numero = entity.Numero;
            Title = _locale.Tf("CmdProd_EditTitleFmt", Numero);
            OnPropertyChanged(nameof(CanAddOperation));

            await _dialog.ShowInfoAsync(Title, _locale.T("CmdProd_Saved"), cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenLinkedBonReception()
    {
        if (LinkedBonReceptionId is not { } brId)
            return;

        var vm = _sp.GetRequiredService<BREditViewModel>();
        vm.Load(brId);
        _workspace.Open(vm);
    }

    private async Task RefreshLinkedBonReceptionAsync(AppDbContext db, int commandeId, CancellationToken cancellationToken)
    {
        var br = await db.BonsReception.AsNoTracking()
            .FirstOrDefaultAsync(b => b.CommandeProductionId == commandeId, cancellationToken);
        SetLinkedBonReception(br?.Numero, br?.Id);
    }

    private void SetLinkedBonReception(string? numero, int? id)
    {
        LinkedBonReceptionId = id;
        LinkedBonReceptionLabel = id is > 0 && !string.IsNullOrWhiteSpace(numero)
            ? _locale.Tf("CmdProd_LinkedBr", numero)
            : string.Empty;
    }

    [RelayCommand]
    private async Task AddPanelTypeHuitreAsync(CancellationToken cancellationToken) =>
        await AddLookupViaPanelCoreAsync(
            NewTypeHuitreNom,
            _locale.T("TypeHuitre_ErrNom"),
            _locale.T("TypeHuitre_ErrDuplicate"),
            async (db, nom, ct) =>
            {
                if (await db.TypesHuitre.AsNoTracking().AnyAsync(t => t.Nom == nom, ct))
                    return -1;
                var maxOrdre = await db.TypesHuitre.AsNoTracking().Select(t => (int?)t.Ordre).MaxAsync(ct) ?? 0;
                var entity = new TypeHuitre
                {
                    Nom = nom,
                    Actif = true,
                    Ordre = maxOrdre + 1,
                    CreatedByUserId = _session.UserId
                };
                db.TypesHuitre.Add(entity);
                await db.SaveChangesAsync(ct);
                return entity.Id;
            },
            () => NewTypeHuitreNom = string.Empty,
            isType: true,
            cancellationToken);

    [RelayCommand]
    private async Task AddPanelCategorieCommandeAsync(CancellationToken cancellationToken) =>
        await AddLookupViaPanelCoreAsync(
            NewCategorieCommandeNom,
            _locale.T("CatCmd_ErrNom"),
            _locale.T("CatCmd_ErrDuplicate"),
            async (db, nom, ct) =>
            {
                if (await db.CategoriesCommande.AsNoTracking().AnyAsync(c => c.Nom == nom, ct))
                    return -1;
                var maxOrdre = await db.CategoriesCommande.AsNoTracking().Select(c => (int?)c.Ordre).MaxAsync(ct) ?? 0;
                var entity = new CategorieCommande
                {
                    Nom = nom,
                    Actif = true,
                    Ordre = maxOrdre + 1,
                    CreatedByUserId = _session.UserId
                };
                db.CategoriesCommande.Add(entity);
                await db.SaveChangesAsync(ct);
                return entity.Id;
            },
            () => NewCategorieCommandeNom = string.Empty,
            isType: false,
            cancellationToken);

    [RelayCommand]
    private async Task EditPanelTypeHuitreAsync(CancellationToken cancellationToken)
    {
        if (SelectedPanelTypeHuitre == null) return;
        await EditLookupAsync(
            SelectedPanelTypeHuitre,
            _locale.Tf("TypeHuitre_TitleFmt", SelectedPanelTypeHuitre.Nom),
            _locale.T("TypeHuitre_ErrNom"),
            _locale.T("TypeHuitre_ErrDuplicate"),
            cancellationToken);
    }

    [RelayCommand]
    private async Task EditPanelCategorieCommandeAsync(CancellationToken cancellationToken)
    {
        if (SelectedPanelCategorieCommande == null) return;
        await EditLookupAsync(
            SelectedPanelCategorieCommande,
            _locale.Tf("CatCmd_TitleFmt", SelectedPanelCategorieCommande.Nom),
            _locale.T("CatCmd_ErrNom"),
            _locale.T("CatCmd_ErrDuplicate"),
            cancellationToken);
    }

    [RelayCommand]
    private async Task DeletePanelTypeHuitreAsync(TypeHuitre? item, CancellationToken cancellationToken)
    {
        if (item == null) return;
        await DeleteLookupAsync(
            item.Id,
            item.Nom,
            _locale.T("TypeHuitre_MenuDelete"),
            _locale.T("TypeHuitre_ConfirmDelete"),
            _locale.T("TypeHuitre_ErrInUse"),
            db => db.CommandesProduction.AsNoTracking().AnyAsync(c => c.TypeHuitreId == item.Id, cancellationToken),
            db => db.TypesHuitre.FirstOrDefaultAsync(t => t.Id == item.Id, cancellationToken),
            (db, entity) => db.TypesHuitre.Remove(entity),
            wasTypeSelected: SelectedTypeHuitre?.Id == item.Id,
            wasCategorieSelected: false,
            cancellationToken);
    }

    [RelayCommand]
    private async Task DeletePanelCategorieCommandeAsync(CategorieCommande? item, CancellationToken cancellationToken)
    {
        if (item == null) return;
        await DeleteLookupAsync(
            item.Id,
            item.Nom,
            _locale.T("CatCmd_MenuDelete"),
            _locale.T("CatCmd_ConfirmDelete"),
            _locale.T("CatCmd_ErrInUse"),
            db => db.CommandesProduction.AsNoTracking().AnyAsync(c => c.CategorieCommandeId == item.Id, cancellationToken),
            db => db.CategoriesCommande.FirstOrDefaultAsync(c => c.Id == item.Id, cancellationToken),
            (db, entity) => db.CategoriesCommande.Remove(entity),
            wasTypeSelected: false,
            wasCategorieSelected: SelectedCategorieCommande?.Id == item.Id,
            cancellationToken);
    }

    private async Task AddLookupViaPanelCoreAsync(
        string rawNom,
        string errNom,
        string errDuplicate,
        Func<AppDbContext, string, CancellationToken, Task<int>> add,
        Action clearNom,
        bool isType,
        CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;
        if (string.IsNullOrWhiteSpace(rawNom))
        {
            await _dialog.ShowErrorAsync(Title, errNom, cancellationToken);
            return;
        }

        var nom = rawNom.Trim();
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var newId = await add(db, nom, cancellationToken);
            if (newId < 0)
            {
                await _dialog.ShowErrorAsync(Title, errDuplicate, cancellationToken);
                return;
            }

            clearNom();
            await LoadLookupsAsync(db, cancellationToken, isType ? newId : null, isType ? null : newId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditLookupAsync(
        object initial,
        string title,
        string errNom,
        string errDuplicate,
        CancellationToken cancellationToken)
    {
        if (!_session.CanAccessProduction) return;

        string nom;
        bool actif;
        int id;
        if (initial is TypeHuitre type)
        {
            nom = type.Nom;
            actif = type.Actif;
            id = type.Id;
        }
        else if (initial is CategorieCommande cat)
        {
            nom = cat.Nom;
            actif = cat.Actif;
            id = cat.Id;
        }
        else return;

        var result = await _dialog.ShowCategorieChargeEditAsync(
            title,
            _locale.T("Wm_Nom"),
            _locale.T("Lbl_Actif"),
            _locale.T("Btn_Cancel"),
            _locale.T("Btn_Save"),
            nom,
            actif,
            cancellationToken);
        if (result == null) return;

        var newNom = result.Nom.Trim();
        if (string.IsNullOrWhiteSpace(newNom))
        {
            await _dialog.ShowErrorAsync(Title, errNom, cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (initial is TypeHuitre)
            {
                if (await db.TypesHuitre.AsNoTracking().AnyAsync(t => t.Nom == newNom && t.Id != id, cancellationToken))
                {
                    await _dialog.ShowErrorAsync(Title, errDuplicate, cancellationToken);
                    return;
                }

                var entity = await db.TypesHuitre.FirstAsync(t => t.Id == id, cancellationToken);
                entity.Nom = newNom;
                entity.Actif = result.Actif;
                await db.SaveChangesAsync(cancellationToken);
                await LoadLookupsAsync(db, cancellationToken, id, null);
            }
            else
            {
                if (await db.CategoriesCommande.AsNoTracking().AnyAsync(c => c.Nom == newNom && c.Id != id, cancellationToken))
                {
                    await _dialog.ShowErrorAsync(Title, errDuplicate, cancellationToken);
                    return;
                }

                var entity = await db.CategoriesCommande.FirstAsync(c => c.Id == id, cancellationToken);
                entity.Nom = newNom;
                entity.Actif = result.Actif;
                await db.SaveChangesAsync(cancellationToken);
                await LoadLookupsAsync(db, cancellationToken, null, id);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteLookupAsync<T>(
        int id,
        string nom,
        string title,
        string confirmKey,
        string inUseKey,
        Func<AppDbContext, Task<bool>> isInUse,
        Func<AppDbContext, Task<T?>> find,
        Action<AppDbContext, T> remove,
        bool wasTypeSelected,
        bool wasCategorieSelected,
        CancellationToken cancellationToken) where T : class
    {
        if (!_session.CanAccessProduction) return;

        var ok = await _dialog.ConfirmAsync(title, _locale.Tf(confirmKey, nom), cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (await isInUse(db))
            {
                await _dialog.ShowErrorAsync(Title, inUseKey, cancellationToken);
                return;
            }

            var entity = await find(db);
            if (entity == null) return;

            remove(db, entity);
            await db.SaveChangesAsync(cancellationToken);

            int? selectType = wasTypeSelected ? null : SelectedTypeHuitre?.Id;
            int? selectCat = wasCategorieSelected ? null : SelectedCategorieCommande?.Id;
            await LoadLookupsAsync(db, cancellationToken, selectType, selectCat);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddOperationAsync(CancellationToken cancellationToken)
    {
        if (CommandeId == null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("CmdProd_ErrSaveFirst"), cancellationToken);
            return;
        }

        await ShowOperationDialogAsync(null, cancellationToken);
    }

    [RelayCommand]
    private async Task EditSelectedOperationAsync(CancellationToken cancellationToken)
    {
        if (SelectedOperation == null) return;
        await ShowOperationDialogAsync(SelectedOperation, cancellationToken);
    }

    [RelayCommand]
    private async Task EditOperationAsync(ProductionOperation? operation, CancellationToken cancellationToken)
    {
        if (operation == null) return;
        await ShowOperationDialogAsync(operation, cancellationToken);
    }

    [RelayCommand]
    private async Task DeleteOperationAsync(ProductionOperation? operation, CancellationToken cancellationToken)
    {
        if (operation == null || CommandeId == null || !_session.CanAccessProduction) return;

        var ok = await _dialog.ConfirmAsync(
            Title,
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

        Operations.Remove(operation);
        RefreshOperationTotals();
    }

    private async Task ShowOperationDialogAsync(ProductionOperation? existing, CancellationToken cancellationToken)
    {
        if (CommandeId == null) return;

        var title = existing == null
            ? _locale.T("Prod_NewTitle")
            : _locale.Tf("Prod_EditTitleFmt", existing.OperationTitle);

        var sumGrandExcluding = existing == null
            ? SumGrandHuitres
            : SumGrandHuitres - existing.TotalGrand;
        var maxRemainingAtWater = ProductionOperation.ComputeRemainingHuitresAtWater(
            QuantiteNaissain,
            sumGrandExcluding);

        var result = await _dialog.ShowProductionOperationEditAsync(
            title,
            _locale.T("Prod_ColTables"),
            _locale.T("Prod_LblGrandPochets"),
            _locale.T("Prod_LblMoyennePochets"),
            _locale.T("Prod_LblPetitPochets"),
            _locale.T("Prod_LblTotalPreview"),
            _locale.T("Prod_OperationMaxRemainingWaterFmt"),
            _locale.T("Prod_OperationExceedsRemainingWater"),
            _locale.T("Btn_Cancel"),
            _locale.T("Btn_Save"),
            existing?.Tables ?? 0,
            existing?.PochetteGrand ?? 0,
            existing?.PochetteMoyenne ?? 0,
            existing?.PochettePetit ?? 0,
            maxRemainingAtWater,
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
        OperationProduction entity;

        if (existing == null)
        {
            entity = new OperationProduction { CommandeProductionId = CommandeId };
            vm.ApplyTo(entity, savedAt);
            db.OperationsProduction.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            entity = await db.OperationsProduction.FirstAsync(o => o.Id == existing.Id, cancellationToken);
            vm.ApplyTo(entity);
        }

        await _productionStock.SyncOperationStockAsync(
            db,
            entity.Id,
            vm.PochetteGrand,
            entity.OperationAt,
            _session.UserId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var mapped = MapOperation(entity);
        if (existing == null)
            Operations.Insert(0, mapped);
        else
        {
            var idx = Operations.IndexOf(existing);
            if (idx >= 0)
                Operations[idx] = mapped;
        }

        RefreshOperationTotals();
    }
}
