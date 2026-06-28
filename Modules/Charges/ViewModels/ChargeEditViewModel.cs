using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.Tiers.Models;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ChargeEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ILocaleService _locale;
    private readonly IDocumentNumberService _numbers;
    private readonly ICurrentUserSession _session;

    public ChargeEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ILocaleService locale,
        IDocumentNumberService numbers,
        ICurrentUserSession session)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale = locale;
        _numbers = numbers;
        _session = session;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
    }

    public ObservableCollection<CategorieCharge> Categories { get; } = [];
    public ObservableCollection<CategorieCharge> AllCategories { get; } = [];
    public ObservableCollection<TiersEntity> Fournisseurs { get; } = [];
    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;

    [ObservableProperty] private int? _chargeId;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private CategorieCharge? _selectedCategorie;
    [ObservableProperty] private int _categorieChargeId;
    [ObservableProperty] private string _libelle = string.Empty;
    [ObservableProperty] private TiersEntity? _selectedFournisseur;
    [ObservableProperty] private int? _fournisseurId;
    [ObservableProperty] private string _fournisseur = string.Empty;
    [ObservableProperty] private decimal _montantTtc;
    [ObservableProperty] private bool _estPayee;
    [ObservableProperty] private string _note = string.Empty;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _lblCategorie = string.Empty;
    [ObservableProperty] private string _lblDate = string.Empty;
    [ObservableProperty] private string _lblLibelle = string.Empty;
    [ObservableProperty] private string _lblFournisseur = string.Empty;
    [ObservableProperty] private string _wmFournisseurSearch = string.Empty;
    [ObservableProperty] private string _lblFournisseurLibre = string.Empty;
    [ObservableProperty] private string _wmFournisseurLibre = string.Empty;
    [ObservableProperty] private string _lblMontant = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _lblFactPayee = string.Empty;
    [ObservableProperty] private string _lblPaid = string.Empty;
    [ObservableProperty] private string _lblUnpaid = string.Empty;
    [ObservableProperty] private string _menuDeleteCharge = string.Empty;
    [ObservableProperty] private bool _showDelete;
    [ObservableProperty] private string _newCategoryNom = string.Empty;
    [ObservableProperty] private CategorieCharge? _selectedPanelCategory;
    [ObservableProperty] private string _lblTypesPanel = string.Empty;
    [ObservableProperty] private string _wmNewCategory = string.Empty;
    [ObservableProperty] private string _btnAddCategory = string.Empty;
    [ObservableProperty] private string _menuDeleteCategory = string.Empty;
    [ObservableProperty] private string _colCategoryNom = string.Empty;
    [ObservableProperty] private string _colCategoryActif = string.Empty;

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_BackList");
        BtnSave = _locale.T("Btn_Save");
        LblCategorie = _locale.T("Chg_ColCategorie");
        LblDate = _locale.T("DevisList_ColDate");
        LblLibelle = _locale.T("Chg_ColLibelle");
        LblFournisseur = _locale.T("Lbl_Supplier");
        WmFournisseurSearch = _locale.T("Wm_SearchSupplier");
        LblFournisseurLibre = _locale.T("Chg_LblBeneficiaireLibre");
        WmFournisseurLibre = _locale.T("Chg_WmBeneficiaireLibre");
        LblMontant = _locale.T("DevisList_ColTtc");
        LblNote = _locale.T("DevisList_ColNote");
        LblFactPayee = _locale.T("Faf_LblPayee");
        LblPaid = _locale.T("Chg_Paid");
        LblUnpaid = _locale.T("Chg_Unpaid");
        MenuDeleteCharge = _locale.T("Chg_MenuDelete");
        LblTypesPanel = _locale.T("Chg_TypesPanel");
        WmNewCategory = _locale.T("Chg_WmNewCategory");
        BtnAddCategory = _locale.T("Chg_BtnAddCategory");
        MenuDeleteCategory = _locale.T("Chg_MenuDeleteCategory");
        ColCategoryNom = _locale.T("Lbl_ColNom");
        ColCategoryActif = _locale.T("Lbl_ColActif");
    }

    partial void OnSelectedCategorieChanged(CategorieCharge? value)
    {
        var id = value?.Id ?? 0;
        if (CategorieChargeId == id) return;
        CategorieChargeId = id;
    }

    partial void OnCategorieChargeIdChanged(int value)
    {
        if (SelectedCategorie?.Id == value) return;
        SelectedCategorie = Categories.FirstOrDefault(c => c.Id == value);
    }

    partial void OnSelectedFournisseurChanged(TiersEntity? value)
    {
        var id = value?.Id;
        if (FournisseurId == id) return;
        FournisseurId = id;
        if (value != null && string.IsNullOrWhiteSpace(Fournisseur))
            Fournisseur = value.Nom;
    }

    partial void OnFournisseurIdChanged(int? value)
    {
        if (SelectedFournisseur?.Id == value) return;
        SelectedFournisseur = value is { } id ? Fournisseurs.FirstOrDefault(f => f.Id == id) : null;
    }

    partial void OnChargeIdChanged(int? value) => ShowDelete = value.HasValue;

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        ChargeId = id;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (id == null)
        {
            Numero = _locale.T("Chg_NewNumPlaceholder");
            Date = new DateTimeOffset(DateTime.Today);
            Libelle = string.Empty;
            SelectedFournisseur = null;
            FournisseurId = null;
            Fournisseur = string.Empty;
            MontantTtc = 0;
            EstPayee = false;
            Note = string.Empty;
            CategorieChargeId = 0;
            Title = _locale.T("Chg_NewTitle");
            await LoadLookupsAsync(db, cancellationToken);
            SelectedCategorie = Categories.FirstOrDefault();
            CategorieChargeId = SelectedCategorie?.Id ?? 0;
            return;
        }

        var c = await db.Charges.AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
        Numero = c.Numero;
        Date = new DateTimeOffset(c.Date);
        CategorieChargeId = c.CategorieChargeId;
        Libelle = c.Libelle;
        FournisseurId = c.FournisseurId;
        Fournisseur = c.Fournisseur;
        MontantTtc = c.MontantTtc;
        EstPayee = c.EstPayee;
        Note = c.Note;
        Title = _locale.Tf("Chg_TitleNum", Numero);
        await LoadLookupsAsync(db, cancellationToken);
        SelectedCategorie = Categories.FirstOrDefault(x => x.Id == c.CategorieChargeId);
        SelectedFournisseur = c.FournisseurId is { } fid ? Fournisseurs.FirstOrDefault(f => f.Id == fid) : null;
    }

    private async Task LoadLookupsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        Categories.Clear();
        AllCategories.Clear();

        var allCats = await db.CategoriesCharges.AsNoTracking()
            .OrderBy(x => x.Nom)
            .ToListAsync(cancellationToken);

        foreach (var c in allCats)
            AllCategories.Add(c);

        foreach (var c in allCats.Where(x => x.Actif))
            Categories.Add(c);

        if (CategorieChargeId > 0 && Categories.All(c => c.Id != CategorieChargeId))
        {
            var current = allCats.FirstOrDefault(x => x.Id == CategorieChargeId);
            if (current != null)
                Categories.Insert(0, current);
        }

        SelectedPanelCategory = AllCategories.FirstOrDefault(c => c.Id == CategorieChargeId)
            ?? AllCategories.FirstOrDefault(c => c.Id == SelectedCategorie?.Id);

        Fournisseurs.Clear();
        foreach (var f in await db.Tiers.AsNoTracking()
                     .Where(t => t.Actif && (t.Type == TypeTiers.Fournisseur || t.Type == TypeTiers.LesDeux))
                     .OrderBy(t => t.Nom)
                     .ToListAsync(cancellationToken))
            Fournisseurs.Add(f);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessCharges) return;
        if (CategorieChargeId <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("Chg_ErrCategorie"), cancellationToken);
            return;
        }

        if (MontantTtc <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("Chg_ErrMontant"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Charge entity;
            if (ChargeId == null)
            {
                var num = await _numbers.NextChargeAsync(cancellationToken);
                entity = new Charge
                {
                    Numero = num,
                    Date = Date.DateTime,
                    CategorieChargeId = CategorieChargeId,
                    Libelle = Libelle.Trim(),
                    FournisseurId = FournisseurId,
                    Fournisseur = ResolveFournisseurText(),
                    MontantTtc = MontantTtc,
                    EstPayee = EstPayee,
                    Note = Note.Trim(),
                    CreatedByUserId = _session.UserId
                };
                db.Charges.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                ChargeId = entity.Id;
            }
            else
            {
                entity = await db.Charges.FirstAsync(c => c.Id == ChargeId, cancellationToken);
                entity.Date = Date.DateTime;
                entity.CategorieChargeId = CategorieChargeId;
                entity.Libelle = Libelle.Trim();
                entity.FournisseurId = FournisseurId;
                entity.Fournisseur = ResolveFournisseurText();
                entity.MontantTtc = MontantTtc;
                entity.EstPayee = EstPayee;
                entity.Note = Note.Trim();
                await db.SaveChangesAsync(cancellationToken);
            }

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("Chg_Title"), _locale.T("Chg_Saved"), cancellationToken);
            await LoadAsync(ChargeId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string ResolveFournisseurText()
    {
        if (!string.IsNullOrWhiteSpace(Fournisseur))
            return Fournisseur.Trim();
        return SelectedFournisseur?.Nom.Trim() ?? string.Empty;
    }

    [RelayCommand]
    private async Task AddCategoryAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessCharges) return;
        if (string.IsNullOrWhiteSpace(NewCategoryNom))
        {
            await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("CategorieCharge_ErrNom"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = new CategorieCharge
            {
                Nom = NewCategoryNom.Trim(),
                Actif = true,
                CreatedByUserId = _session.UserId
            };
            db.CategoriesCharges.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            NewCategoryNom = string.Empty;
            await LoadLookupsAsync(db, cancellationToken);
            SelectedCategorie = Categories.FirstOrDefault(c => c.Id == entity.Id)
                ?? AllCategories.FirstOrDefault(c => c.Id == entity.Id);
            CategorieChargeId = entity.Id;
            SelectedPanelCategory = AllCategories.FirstOrDefault(c => c.Id == entity.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditPanelCategoryAsync(CancellationToken cancellationToken)
    {
        if (SelectedPanelCategory == null) return;

        var initial = SelectedPanelCategory;
        var result = await _dialog.ShowCategorieChargeEditAsync(
            _locale.Tf("CategorieCharge_TitleFmt", initial.Nom),
            _locale.T("Wm_Nom"),
            _locale.T("Lbl_Actif"),
            _locale.T("Btn_Cancel"),
            _locale.T("Btn_Save"),
            initial.Nom,
            initial.Actif,
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
            var entity = await db.CategoriesCharges.FirstAsync(c => c.Id == initial.Id, cancellationToken);
            entity.Nom = result.Nom.Trim();
            entity.Actif = result.Actif;
            await db.SaveChangesAsync(cancellationToken);
            await LoadLookupsAsync(db, cancellationToken);
            SelectedCategorie = Categories.FirstOrDefault(c => c.Id == entity.Id);
            if (SelectedCategorie != null)
                CategorieChargeId = SelectedCategorie.Id;
            SelectedPanelCategory = AllCategories.FirstOrDefault(c => c.Id == entity.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeletePanelCategoryAsync(CategorieCharge? category, CancellationToken cancellationToken)
    {
        if (category == null || !_session.CanAccessCharges) return;

        var ok = await _dialog.ConfirmAsync(
            _locale.T("CategorieCharge_Title"),
            _locale.Tf("Chg_ConfirmDeleteCategory", category.Nom),
            cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (await db.Charges.AsNoTracking().AnyAsync(c => c.CategorieChargeId == category.Id, cancellationToken))
            {
                await _dialog.ShowErrorAsync(_locale.T("Dlg_Validation"), _locale.T("Chg_ErrCategoryInUse"), cancellationToken);
                return;
            }

            var entity = await db.CategoriesCharges.FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);
            if (entity == null) return;

            db.CategoriesCharges.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);

            var wasSelected = CategorieChargeId == category.Id;
            await LoadLookupsAsync(db, cancellationToken);
            if (wasSelected)
            {
                SelectedCategorie = Categories.FirstOrDefault();
                CategorieChargeId = SelectedCategorie?.Id ?? 0;
            }

            SelectedPanelCategory = AllCategories.FirstOrDefault(c => c.Id == CategorieChargeId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveChargeAsync(CancellationToken cancellationToken)
    {
        if (ChargeId is not { } id) return;
        var ok = await _dialog.ConfirmAsync(
            _locale.T("Chg_Title"),
            _locale.Tf("Chg_ConfirmDelete", Numero),
            cancellationToken);
        if (!ok) return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.Charges.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (entity == null) return;
            db.Charges.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            Back();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<ChargeListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }
}
