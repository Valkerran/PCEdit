using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.ViewModels;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Tests;

/// <summary>
/// One real main window per test: fresh services, a private folder for settings, backups and
/// saves, and helpers to drive it. Dispose closes the window and fails the test if an exception
/// reached the UI thread that the test did not say it expected.
/// </summary>
public sealed class UiHarness : IDisposable
{
    private readonly List<Exception> _unexpectedErrors = [];

    public UiHarness(Action<IServiceCollection>? configure = null, bool acknowledgeDisclaimer = true)
    {
        Root = Directory.CreateDirectory(Path.Combine(TestApp.Root, Guid.NewGuid().ToString("N"))).FullName;
        AppPaths.RootOverride = Root;

        // A localizer of its own: the app's is a process-wide singleton that every earlier
        // test's view models are still subscribed to. Text bound in XAML with {m:Loc} still
        // comes from that singleton and stays in its start-up language.
        Localizer = new Localizer();
        Localizer.SetCulture("en-US");
        Services = App.BuildServices(services =>
        {
            services.AddSingleton<ILocalizer>(Localizer);
            configure?.Invoke(services);
        });
        ((App)Application.Current!).Services = Services;
        Window = Services.GetRequiredService<MainWindow>();
        Dispatcher.UIThread.UnhandledException += RecordUnexpectedError;

        try
        {
            Open(acknowledgeDisclaimer);
        }
        catch
        {
            CleanUp();
            throw;
        }
    }

    private void Open(bool acknowledgeDisclaimer)
    {
        if (acknowledgeDisclaimer)
        {
            Services.GetRequiredService<JsonSettingsStore>().Acknowledge();
        }

        Services.GetRequiredService<AvaloniaNavigationService>().Attach(Window);
        Shell.Start();
        Window.Show();
        Pump();
    }

    public string Root { get; }

    public IServiceProvider Services { get; }

    public Localizer Localizer { get; }

    public MainWindow Window { get; }

    public MainWindowViewModel Shell => Services.GetRequiredService<MainWindowViewModel>();

    public ISaveFileWorkspace Workspace => Services.GetRequiredService<ISaveFileWorkspace>();

    /// <summary>Set by a test that provokes an unexpected error on purpose.</summary>
    public bool ExpectUnexpectedError { get; set; }

    /// <summary>The message dialogs open over the main window, oldest first.</summary>
    public IReadOnlyList<MessageDialog> Dialogs => [.. Window.OwnedWindows.OfType<MessageDialog>()];

    /// <summary>Runs everything queued on the UI thread: bindings, posted work, dialog opens.</summary>
    public void Pump() => Dispatcher.UIThread.RunJobs();

    public T Find<T>(string name) where T : Control =>
        Window.FindControl<T>(name) ?? throw new InvalidOperationException($"No {typeof(T).Name} named '{name}'.");

    public static string? TitleOf(MessageDialog dialog) => dialog.FindControl<TextBlock>("TitleText")?.Text;

    public static string? MessageOf(MessageDialog dialog) => dialog.FindControl<TextBlock>("MessageText")?.Text;

    /// <summary>Presses a dialog button by position, left to right.</summary>
    public void Click(MessageDialog dialog, int buttonIndex)
    {
        var button = dialog.FindControl<StackPanel>("ButtonBar")!.Children.OfType<Button>().ElementAt(buttonIndex);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();
    }

    /// <summary>Copies the small fixture save into this test's folder and opens it.</summary>
    public string LoadSave()
    {
        var path = Path.Combine(Root, "Survival-1.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json"), path);
        Workspace.Load(path);
        Pump();
        return path;
    }

    public void MakeAnEdit()
    {
        Workspace.MutateUnlocks(unlocks => unlocks with { TerraTokens = unlocks.TerraTokens + 1 });
        Pump();
    }

    public void ClickSave()
    {
        Find<Button>("SaveButton").Command!.Execute(null);
        Pump();
    }

    public void Dispose()
    {
        CleanUp();

        if (_unexpectedErrors.Count > 0 && !ExpectUnexpectedError)
        {
            throw new InvalidOperationException(
                "An exception reached the UI thread during this test.", _unexpectedErrors[0]);
        }
    }

    /// <summary>Leaves nothing behind for the next test, whatever state this one ended in.</summary>
    private void CleanUp()
    {
        Dispatcher.UIThread.UnhandledException -= RecordUnexpectedError;
        try
        {
            // Unloaded first, or closing a window with unsaved edits opens the discard prompt.
            Workspace.Close();
            foreach (var dialog in Dialogs)
            {
                dialog.Close();
            }

            Window.Close();
            Pump();
        }
        finally
        {
            AppPaths.RootOverride = TestApp.Root;
            DeleteRoot();
        }
    }

    private void DeleteRoot()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file still held open (Windows) must not replace the test's own result; the run's
            // root is removed when the process exits.
        }
    }

    // The app's own handler marks these handled and carries on, which is right for a player and
    // wrong for a test: without this a broken test would pass.
    private void RecordUnexpectedError(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        _unexpectedErrors.Add(e.Exception);
}
