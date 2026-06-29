using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.ViewModels;

public partial class MainWindowViewModel : BaseViewModel
{
    public MainWindowViewModel(RootNavigator rootNavigator)
    {
        Root = rootNavigator;
        Title = AppInfo.WindowTitle;
    }

    public RootNavigator Root { get; }
}
