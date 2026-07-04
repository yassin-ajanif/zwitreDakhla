using System;
using Avalonia.Controls;
using Avalonia.Threading;
using GestionCommerciale.Modules.Production.ViewModels;

namespace GestionCommerciale.Modules.Production.Views;

public partial class CommandeProductionEditView : UserControl
{
    private CommandeProductionEditViewModel? _viewModel;

    public CommandeProductionEditView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
    }

    private void AttachViewModel()
    {
        if (_viewModel != null)
            _viewModel.ScrollToOperationRequested -= OnScrollToOperationRequested;

        _viewModel = DataContext as CommandeProductionEditViewModel;
        if (_viewModel != null)
            _viewModel.ScrollToOperationRequested += OnScrollToOperationRequested;
    }

    private void OnScrollToOperationRequested(int operationId)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await System.Threading.Tasks.Task.Delay(80);
            if (_viewModel?.SelectedOperation == null)
                return;

            var container = OperationsListBox.ContainerFromItem(_viewModel.SelectedOperation);
            if (container is Control control)
                control.BringIntoView();
        });
    }
}
