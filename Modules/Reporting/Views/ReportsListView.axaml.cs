using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using GestionCommerciale.Modules.Reporting.ViewModels;

namespace GestionCommerciale.Modules.Reporting.Views;

public partial class ReportsListView : UserControl
{
    public ReportsListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnProfitSummaryHostPressed(object? sender, PointerPressedEventArgs e)
    {
        for (var visual = e.Source as Visual; visual != null; visual = visual.GetVisualParent())
        {
            if (visual is Button)
                return;
            if (ReferenceEquals(visual, sender))
                break;
        }

        if (DataContext is ReportsListViewModel vm)
            vm.ClearProfitChargesFilterCommand.Execute(null);
    }
}
