using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.ViewModels;

namespace PCEdit.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly IDisclaimerGate? _disclaimerGate;
    private readonly IDialogService? _dialogs;
    private readonly ILocalizer? _localizer;
    private readonly TextBlock _liveRegion;
    private bool _disclaimerChecked;
    private bool _forceClose;

    // Design-time / XAML previewer.
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _liveRegion = this.FindControl<TextBlock>("LiveRegion")!;
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        IDisclaimerGate disclaimerGate,
        IDialogService dialogs,
        ILocalizer localizer,
        IScreenReaderAnnouncer announcer)
        : this()
    {
        DataContext = viewModel;
        _disclaimerGate = disclaimerGate;
        _dialogs = dialogs;
        _localizer = localizer;

        if (announcer is AvaloniaScreenReaderAnnouncer avaloniaAnnouncer)
        {
            avaloniaAnnouncer.Sink = message => _liveRegion.Text = message;
        }

        // The platform's command key: Cmd on macOS, Ctrl elsewhere. Avalonia does not map Ctrl to
        // Cmd, so a hard-coded Ctrl+S did nothing for a Mac user (#106).
        var commandKey = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.S, commandKey), Command = viewModel.SaveCommand });

        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || _dialogs is null || _localizer is null ||
            DataContext is not MainWindowViewModel { Workspace.IsDirty: true })
        {
            return;
        }

        e.Cancel = true;

        var discard = await _dialogs.ConfirmAsync(
            _localizer[LocKeys.Quit_DiscardTitle],
            _localizer[LocKeys.Quit_DiscardBody],
            _localizer[LocKeys.Common_CloseWithoutSaving],
            _localizer[LocKeys.Common_KeepEditing]);

        if (discard)
        {
            _forceClose = true;
            Close();
        }
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;

        if (!_disclaimerChecked && _disclaimerGate is { HasAcknowledged: false })
        {
            if (!await TryShowDisclaimerAsync())
            {
                return;
            }

            _disclaimerGate.Acknowledge();
        }

        _disclaimerChecked = true;

        // Build the other pages' views now, while the app is idle, so the first navigation to each
        // isn't a visible stall (XAML load + template JIT happens here instead of on the click).
        Dispatcher.UIThread.Post(PrewarmPageViews, DispatcherPriority.Background);
    }

    /// <summary>
    /// The disclaimer is a gate: unexpected errors no longer end the app, so one raised while
    /// showing it would otherwise leave PCEdit usable without it ever having been seen.
    /// </summary>
    private async Task<bool> TryShowDisclaimerAsync()
    {
        try
        {
            await _dialogs!.ShowDisclaimerAsync(
                _localizer![LocKeys.Disclaimer_Title],
                _localizer[LocKeys.Disclaimer_Body],
                _localizer[LocKeys.Disclaimer_Acknowledge]);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not show the disclaimer: {ex}");
            _forceClose = true;
            Close();
            return false;
        }
    }

    private void PrewarmPageViews()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var locator = Application.Current?.DataTemplates.OfType<PCEdit.Desktop.ViewLocator>().FirstOrDefault();
        if (locator is null)
        {
            return;
        }

        foreach (var pageViewModel in viewModel.PageViewModels)
        {
            locator.Build(pageViewModel);
        }
    }
}
