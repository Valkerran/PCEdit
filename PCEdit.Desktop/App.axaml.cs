using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.ViewModels;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.ViewModels;
using PCEdit.Desktop.Views;
using PCEdit.SaveFileHandler;

namespace PCEdit.Desktop;

public partial class App : Application
{
    public IServiceProvider Services { get; internal set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        LogFile.Start(AppPaths.Log);
        LogWhatCannotBeRecovered();

        Services = BuildServices();

        // The Translate markup extension binds to Loc.Instance, which is the object we register in DI.
        var localizer = Services.GetRequiredService<ILocalizer>();
        var languageStore = Services.GetRequiredService<ILanguageStore>();
        localizer.SetCulture(LanguageStartup.ResolveCulture(languageStore, localizer.AvailableLocales));

        KeepRunningAfterAnUnexpectedError();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            Services.GetRequiredService<AvaloniaNavigationService>().Attach(mainWindow);
            desktop.MainWindow = mainWindow;
            Services.GetRequiredService<MainWindowViewModel>().Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Off the UI thread there is nothing to hand the error to, so it is only logged:
    /// the first of these ends the process whatever is done here.</summary>
    private static void LogWhatCannotBeRecovered()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Trace.WriteLine($"{DateTime.UtcNow:O} Fatal error: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Trace.WriteLine($"{DateTime.UtcNow:O} Unobserved task error: {e.Exception}");
            e.SetObserved();
        };
    }

    /// <summary>
    /// An exception escaping a command or an event handler on the UI thread used to end the
    /// process, taking the open save's unsaved edits with it and leaving nothing to diagnose
    /// (issue #74). It is now logged and shown, and the app carries on. The reporter is taken
    /// from <see cref="Services"/> when the error happens, not captured here, so it always shows
    /// its dialog over the window that is current.
    /// </summary>
    private void KeepRunningAfterAnUnexpectedError()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            try
            {
                _ = Services.GetRequiredService<UnexpectedErrorReporter>().ReportAsync(e.Exception);
            }
            catch (Exception ex)
            {
                // Nothing may escape this handler: that would end the process it exists to keep.
                Trace.WriteLine($"{DateTime.UtcNow:O} Could not report an unexpected error: {ex}; the error was: {e.Exception}");
            }
        };
    }

    /// <param name="configure">Registrations added last, so they replace the defaults. The UI
    /// tests use it to swap in fakes; the app passes nothing.</param>
    internal static IServiceProvider BuildServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        AddSaveFileServices(services);
        AddPlatformServices(services);
        AddViewModels(services);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static void AddSaveFileServices(ServiceCollection services)
    {
        services.AddSingleton<IJsonRecordSerializer, JsonRecordSerializer>();
        services.AddSingleton<IPlanetCrafterSaveFileSerializer, PlanetCrafterSaveFileSerializer>();
        services.AddSingleton<IPlanetCrafterSaveFileStore, PlanetCrafterSaveFileStore>();

        services.AddSingleton<IItemCatalog, ItemCatalog>();
        services.AddSingleton<ILogisticsGroupCatalog, LogisticsGroupCatalog>();
        services.AddSingleton<ISaveBackupService>(_ => new FileSaveBackupService(AppPaths.Backups));
        services.AddSingleton<ISaveFileWorkspace, SaveFileWorkspace>();
        services.AddSingleton<IPlanetIndex, PlanetIndex>();
        services.AddSingleton<IInventoryEditor, InventoryEditor>();
        services.AddSingleton<IOverflowRepair, OverflowRepair>();
    }

    private static void AddPlatformServices(ServiceCollection services)
    {
        services.AddSingleton<ILocalizer>(Loc.Instance);
        services.AddSingleton(_ => new JsonSettingsStore(AppPaths.Settings));
        services.AddSingleton<ILanguageStore>(sp => sp.GetRequiredService<JsonSettingsStore>());
        services.AddSingleton<IDisclaimerGate>(sp => sp.GetRequiredService<JsonSettingsStore>());
        services.AddSingleton<IInventoryDisplayStore>(sp => sp.GetRequiredService<JsonSettingsStore>());
        services.AddSingleton<IAppVersionInfo, AvaloniaAppVersionInfo>();

        services.AddSingleton<MainWindowAccessor>();
        services.AddSingleton<IScreenReaderAnnouncer, AvaloniaScreenReaderAnnouncer>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();

        services.AddSingleton<AvaloniaNavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AvaloniaNavigationService>());
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton(sp => new UnexpectedErrorReporter(
            sp.GetRequiredService<IDialogService>(), sp.GetRequiredService<ILocalizer>(), AppPaths.Log));
    }

    private static void AddViewModels(ServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        // Page view-models are singletons: each re-reads workspace state in Load(), and keeping the
        // instance lets ViewLocator cache its view so re-navigation doesn't rebuild the visual tree.
        services.AddSingleton<OpenFileViewModel>();
        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<InventoriesViewModel>();
        services.AddSingleton<TerraTokensViewModel>();
        services.AddSingleton<TeleportViewModel>();
        services.AddSingleton<AboutViewModel>();

        // Transient: opened per-move as a modal with a fresh Initialize(worldObjectId).
        services.AddTransient<SelectInventoryViewModel>();
        services.AddTransient<LogisticsEditorViewModel>();
        services.AddTransient<RepairViewModel>();
    }
}
