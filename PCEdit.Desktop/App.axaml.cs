using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
    public IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServices();

        // The Translate markup extension binds to Loc.Instance, which is the object we register in DI.
        var localizer = Services.GetRequiredService<ILocalizer>();
        var languageStore = Services.GetRequiredService<ILanguageStore>();
        localizer.SetCulture(LanguageStartup.ResolveCulture(languageStore, localizer.AvailableLocales));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            Services.GetRequiredService<AvaloniaNavigationService>().Attach(mainWindow);
            desktop.MainWindow = mainWindow;
            Services.GetRequiredService<MainWindowViewModel>().Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IJsonRecordSerializer, JsonRecordSerializer>();
        services.AddSingleton<IPlanetCrafterSaveFileSerializer, PlanetCrafterSaveFileSerializer>();
        services.AddSingleton<IPlanetCrafterSaveFileStore, PlanetCrafterSaveFileStore>();

        services.AddSingleton<ILocalizer>(Loc.Instance);
        services.AddSingleton<JsonSettingsStore>();
        services.AddSingleton<ILanguageStore>(sp => sp.GetRequiredService<JsonSettingsStore>());
        services.AddSingleton<IDisclaimerGate>(sp => sp.GetRequiredService<JsonSettingsStore>());
        services.AddSingleton<IAppVersionInfo, AvaloniaAppVersionInfo>();

        services.AddSingleton<MainWindowAccessor>();
        services.AddSingleton<IScreenReaderAnnouncer, AvaloniaScreenReaderAnnouncer>();
        services.AddSingleton<IItemCatalog, ItemCatalog>();
        services.AddSingleton<ILogisticsGroupCatalog, LogisticsGroupCatalog>();
        services.AddSingleton<ISaveFileWorkspace, SaveFileWorkspace>();
        services.AddSingleton<IPlanetIndex, PlanetIndex>();
        services.AddSingleton<IInventoryEditor, InventoryEditor>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();

        services.AddSingleton<AvaloniaNavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AvaloniaNavigationService>());
        services.AddSingleton<IDialogService, AvaloniaDialogService>();

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

        return services.BuildServiceProvider();
    }
}
