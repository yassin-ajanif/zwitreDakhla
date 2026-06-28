using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.ViewModels;

public partial class MainWindowViewModel : BaseViewModel
{
    private readonly ILocaleService _locale;

    public MainWindowViewModel(RootNavigator rootNavigator, ILocaleService locale)
    {
        Root = rootNavigator;
        _locale = locale;
        _locale.CultureApplied += (_, _) => Title = _locale.T("Win_AppTitle");
        Title = _locale.T("Win_AppTitle");
    }

    public RootNavigator Root { get; }
}
