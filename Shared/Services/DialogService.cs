using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace GestionCommerciale.Shared.Services;

public sealed class DialogService : IDialogService
{
    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
            ? d.MainWindow
            : null;

    public async Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken = default, int autoCloseMs = 0)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 260,
            MaxWidth = 440,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });

        var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => w.Close();
        panel.Children.Add(ok);
        w.Content = panel;

        if (autoCloseMs > 0)
            _ = Task.Delay(autoCloseMs, cancellationToken).ContinueWith(_ => w.Close(), TaskScheduler.FromCurrentSynchronizationContext());

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();
    }

    public Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken = default) =>
        ShowInfoAsync(title, message, cancellationToken);

    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 260,
            MaxWidth = 440,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var confirmed = false;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var no = new Button { Content = "Non" };
        no.Click += (_, _) =>
        {
            confirmed = false;
            w.Close();
        };
        var yes = new Button { Content = "Oui", IsDefault = true };
        yes.Click += (_, _) =>
        {
            confirmed = true;
            w.Close();
        };
        buttons.Children.Add(no);
        buttons.Children.Add(yes);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return confirmed;
    }

    public async Task<string?> PromptPasswordAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 300,
            MaxWidth = 460,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? password = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 420
        });

        var input = new TextBox
        {
            PasswordChar = '*',
            MinWidth = 260
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Annuler" };
        cancel.Click += (_, _) =>
        {
            password = null;
            w.Close();
        };
        var ok = new Button { Content = "Valider", IsDefault = true };
        ok.Click += (_, _) =>
        {
            password = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return password;
    }

    public async Task<string?> PromptLicenseAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 320,
            MaxWidth = 480,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? licenseKey = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 440
        });

        var input = new TextBox
        {
            MinWidth = 280,
            Watermark = "Clé de licence"
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Quitter" };
        cancel.Click += (_, _) =>
        {
            licenseKey = null;
            w.Close();
        };
        var ok = new Button { Content = "Activer", IsDefault = true };
        ok.Click += (_, _) =>
        {
            licenseKey = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return licenseKey;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 300,
            MaxWidth = 460,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 420
        });

        var input = new TextBox
        {
            MinWidth = 260
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Annuler" };
        cancel.Click += (_, _) =>
        {
            result = null;
            w.Close();
        };
        var ok = new Button { Content = "Valider", IsDefault = true };
        ok.Click += (_, _) =>
        {
            result = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<List<int>?> ShowBlPickerAsync(string title, IReadOnlyList<(int Id, string Numero, DateTime Date, string MontantLabel)> availableBls, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 400,
            MaxWidth = 600,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        List<int>? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };

        var checkboxes = new List<CheckBox>();
        var listPanel = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(0, 0, 0, 8) };
        foreach (var bl in availableBls)
        {
            var cb = new CheckBox
            {
                Content = $"{bl.Numero}  —  {bl.Date:d}  —  {bl.MontantLabel}",
                Tag = bl.Id
            };
            checkboxes.Add(cb);
            listPanel.Children.Add(cb);
        }

        var listHost = availableBls.Count > 8
            ? (Control)new ScrollViewer { Content = listPanel, MaxHeight = 320 }
            : listPanel;
        panel.Children.Add(listHost);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var btnCancel = new Button { Content = "Annuler" };
        btnCancel.Click += (_, _) => w.Close();
        var btnAdd = new Button { Content = "Ajouter", IsDefault = true };
        btnAdd.Click += (_, _) =>
        {
            result = checkboxes.Where(cb => cb.IsChecked == true).Select(cb => (int)cb.Tag!).ToList();
            w.Close();
        };
        actions.Children.Add(btnCancel);
        actions.Children.Add(btnAdd);
        panel.Children.Add(actions);

        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public Task<List<int>?> ShowBrPickerAsync(string title, IReadOnlyList<(int Id, string Numero, DateTime Date, string MontantLabel)> availableBrs, CancellationToken cancellationToken = default) =>
        ShowBlPickerAsync(title, availableBrs, cancellationToken);

    public async Task<CategorieChargeDialogResult?> ShowCategorieChargeEditAsync(
        string title,
        string nomLabel,
        string actifLabel,
        string cancelLabel,
        string saveLabel,
        string? initialNom = null,
        bool initialActif = true,
        CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 320,
            MaxWidth = 420,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        CategorieChargeDialogResult? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };

        panel.Children.Add(new TextBlock { Text = nomLabel, Opacity = 0.75 });
        var nomInput = new TextBox
        {
            MinWidth = 280,
            Text = initialNom ?? string.Empty,
            Watermark = nomLabel
        };
        panel.Children.Add(nomInput);

        var actifCheck = new CheckBox
        {
            Content = actifLabel,
            IsChecked = initialActif
        };
        panel.Children.Add(actifCheck);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var cancel = new Button { Content = cancelLabel };
        cancel.Click += (_, _) => w.Close();
        var save = new Button { Content = saveLabel, IsDefault = true };
        save.Click += (_, _) =>
        {
            result = new CategorieChargeDialogResult(nomInput.Text ?? string.Empty, actifCheck.IsChecked == true);
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<ProductionOperationDialogResult?> ShowProductionOperationEditAsync(
        string title,
        string tablesLabel,
        string grandLabel,
        string moyenneLabel,
        string petitLabel,
        string totalPreviewLabel,
        string cancelLabel,
        string saveLabel,
        int initialTables,
        int initialGrand,
        int initialMoyenne,
        int initialPetit,
        CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 360,
            MaxWidth = 440,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        ProductionOperationDialogResult? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 10 };

        void AddField(string label, Control input)
        {
            panel.Children.Add(new TextBlock { Text = label, Opacity = 0.75 });
            panel.Children.Add(input);
        }

        var tablesInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 99999,
            Value = initialTables,
            FormatString = "N0"
        };
        AddField(tablesLabel, tablesInput);

        var grandInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999999,
            Value = initialGrand,
            FormatString = "N0"
        };
        AddField(grandLabel, grandInput);

        var moyenneInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999999,
            Value = initialMoyenne,
            FormatString = "N0"
        };
        AddField(moyenneLabel, moyenneInput);

        var petitInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999999,
            Value = initialPetit,
            FormatString = "N0"
        };
        AddField(petitLabel, petitInput);

        var totalPreview = new TextBlock { FontWeight = Avalonia.Media.FontWeight.SemiBold, Opacity = 0.85 };
        void RefreshTotal()
        {
            var g = (int)(grandInput.Value ?? 0);
            var m = (int)(moyenneInput.Value ?? 0);
            var p = (int)(petitInput.Value ?? 0);
            var total = g * 160 + m * 160 + p * 160;
            totalPreview.Text = $"{totalPreviewLabel}: {total:N0}";
        }

        grandInput.ValueChanged += (_, _) => RefreshTotal();
        moyenneInput.ValueChanged += (_, _) => RefreshTotal();
        petitInput.ValueChanged += (_, _) => RefreshTotal();
        RefreshTotal();
        panel.Children.Add(totalPreview);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var cancel = new Button { Content = cancelLabel };
        cancel.Click += (_, _) => w.Close();
        var save = new Button { Content = saveLabel, IsDefault = true };
        save.Click += (_, _) =>
        {
            result = new ProductionOperationDialogResult(
                (int)(tablesInput.Value ?? 0),
                (int)(grandInput.Value ?? 0),
                (int)(moyenneInput.Value ?? 0),
                (int)(petitInput.Value ?? 0));
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<(DateTime from, DateTime to)?> PickDateRangeAsync(
        string title,
        CancellationToken cancellationToken = default,
        DateTime? initialFrom = null,
        DateTime? initialTo = null)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 420,
            MaxWidth = 520,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        (DateTime from, DateTime to)? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };

        var dpFrom = new DatePicker();
        var dpTo = new DatePicker();

        void SetRange(DateTime from, DateTime to)
        {
            dpFrom.SelectedDate = new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, TimeSpan.Zero);
            dpTo.SelectedDate = new DateTimeOffset(to.Year, to.Month, to.Day, 0, 0, 0, TimeSpan.Zero);
        }

        static Button CreateChip(string label, Action onClick)
        {
            var btn = new Button
            {
                Content = label,
                Padding = new Avalonia.Thickness(12, 6),
                Margin = new Avalonia.Thickness(0, 0, 6, 6),
                MinHeight = 32
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        var presets = new WrapPanel
        {
            MaxWidth = 460,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        presets.Children.Add(CreateChip("Aujourd'hui", () => SetRange(DateTime.Today, DateTime.Today)));

        presets.Children.Add(CreateChip("Cette semaine", () =>
        {
            var today = DateTime.Today;
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var monday = today.AddDays(-diff);
            SetRange(monday, monday.AddDays(6));
        }));

        presets.Children.Add(CreateChip("Ce mois", () =>
        {
            var today = DateTime.Today;
            var first = new DateTime(today.Year, today.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            SetRange(first, last);
        }));

        presets.Children.Add(CreateChip("Cette année", () =>
        {
            var today = DateTime.Today;
            SetRange(new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31));
        }));

        panel.Children.Add(presets);

        if (initialFrom.HasValue && initialTo.HasValue)
            SetRange(initialFrom.Value.Date, initialTo.Value.Date);

        var dateGrid = new StackPanel { Spacing = 8 };
        var fromRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        fromRow.Children.Add(new TextBlock { Text = "Du:", VerticalAlignment = VerticalAlignment.Center, MinWidth = 30 });
        dpFrom.MinWidth = 180;
        fromRow.Children.Add(dpFrom);
        dateGrid.Children.Add(fromRow);

        var toRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        toRow.Children.Add(new TextBlock { Text = "Au:", VerticalAlignment = VerticalAlignment.Center, MinWidth = 30 });
        dpTo.MinWidth = 180;
        toRow.Children.Add(dpTo);
        dateGrid.Children.Add(toRow);
        panel.Children.Add(dateGrid);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var btnClear = new Button { Content = "Effacer" };
        btnClear.Click += (_, _) =>
        {
            result = (DateTime.MinValue, DateTime.MinValue);
            w.Close();
        };
        var btnCancel = new Button { Content = "Annuler" };
        btnCancel.Click += (_, _) => w.Close();
        var btnApply = new Button { Content = "Appliquer", IsDefault = true };
        btnApply.Click += (_, _) =>
        {
            if (!dpFrom.SelectedDate.HasValue && !dpTo.SelectedDate.HasValue)
                result = (DateTime.MinValue, DateTime.MinValue);
            else if (dpFrom.SelectedDate.HasValue && dpTo.SelectedDate.HasValue)
                result = (dpFrom.SelectedDate.Value.DateTime.Date, dpTo.SelectedDate.Value.DateTime.Date);
            w.Close();
        };
        actions.Children.Add(btnClear);
        actions.Children.Add(btnCancel);
        actions.Children.Add(btnApply);
        panel.Children.Add(actions);

        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<string?> PickOpenFileAsync(string title, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var filters = new List<FilePickerFileType>
        {
            new(title) { Patterns = patterns.ToList() }
        };

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveFileAsync(string title, string suggestedFileName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(title) { Patterns = patterns.ToList() }
            }
        });

        return file?.TryGetLocalPath();
    }

    public async Task<bool> SavePickedFileBytesAsync(string title, string suggestedFileName, IReadOnlyList<string> patterns, byte[] content, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return false;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(title) { Patterns = patterns.ToList() }
            }
        });

        if (file == null) return false;

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return true;
    }
}
