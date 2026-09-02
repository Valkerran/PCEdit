using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.Desktop.ViewModels;

public enum NavDestination
{
    OpenFile,
    Overview,
    Inventories,
    TerraTokens,
    Teleport,
    About,
}

public sealed partial class NavItem(NavDestination destination, string label) : ObservableObject
{
    public NavDestination Destination { get; } = destination;

    [ObservableProperty]
    private string _label = label;
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ILocalizer _localizer;
    private readonly ILanguageStore _languageStore;
    private readonly IScreenReaderAnnouncer _announcer;

    // Skip a page's Load() when nothing it reads has changed since it last loaded.
    private readonly Dictionary<NavDestination, int> _loadedAtRevision = new();
    private int _workspaceRevision;

    // The first SelectedNavItem assignment is the app's initial landing, not a user navigation —
    // don't announce it (a modal disclaimer may be up). Every change after that is announced.
    private bool _navAnnouncementsArmed;

    public MainWindowViewModel(
        IServiceProvider services,
        ILocalizer localizer,
        ILanguageStore languageStore,
        ISaveFileWorkspace workspace,
        IScreenReaderAnnouncer announcer)
    {
        _services = services;
        _localizer = localizer;
        _languageStore = languageStore;
        _announcer = announcer;
        Workspace = workspace;

        NavItems =
        [
            new NavItem(NavDestination.OpenFile, string.Empty),
            new NavItem(NavDestination.Overview, string.Empty),
            new NavItem(NavDestination.Inventories, string.Empty),
            new NavItem(NavDestination.TerraTokens, string.Empty),
            new NavItem(NavDestination.Teleport, string.Empty),
            new NavItem(NavDestination.About, string.Empty),
        ];
        RefreshLabels();

        _selectedLanguage = MatchLanguage();
        _localizer.CultureChanged += (_, _) =>
        {
            RefreshLabels();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(DirtyText));

            // {m:Loc} bindings re-read themselves, but localized text baked into page view-models
            // (option lists, computed labels) only refreshes on Load() — force every page to reload.
            _loadedAtRevision.Clear();
            if (SelectedNavItem is not null)
            {
                Show(SelectedNavItem.Destination, force: true);
            }
        };

        workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ISaveFileWorkspace.IsDirty) or null)
            {
                OnPropertyChanged(nameof(DirtyText));
            }

            if (e.PropertyName is nameof(ISaveFileWorkspace.Current)
                or nameof(ISaveFileWorkspace.IsLoaded) or null)
            {
                _workspaceRevision++;
            }
        };
    }

    /// <summary>Navigates to the landing page. Called after the container is fully built.</summary>
    public void Start() => SelectedNavItem = NavItems[0];

    public ISaveFileWorkspace Workspace { get; }

    public string DirtyText =>
        _localizer[Workspace.IsDirty ? LocKeys.Dirty_Unsaved : LocKeys.Dirty_Saved];

    public ObservableCollection<NavItem> NavItems { get; }

    public IReadOnlyList<LocaleOption> Languages => _localizer.AvailableLocales;

    public string Title => _localizer[LocKeys.Shell_Title];

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private object? _currentPage;

    /// <summary>Set when a page's Load() threw, so the shell can say so instead of dying.</summary>
    [ObservableProperty]
    private string? _pageError;

    [ObservableProperty]
    private LocaleOption? _selectedLanguage;

    public void NavigateTo(NavDestination destination)
    {
        var item = NavItems.FirstOrDefault(n => n.Destination == destination);
        if (item is null)
        {
            return;
        }

        if (!ReferenceEquals(item, SelectedNavItem))
        {
            SelectedNavItem = item;
        }
        else
        {
            Show(destination);
        }
    }

    public void ReloadCurrent()
    {
        if (SelectedNavItem is not null)
        {
            Show(SelectedNavItem.Destination, force: true);
        }
    }

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is null)
        {
            return;
        }

        Show(value.Destination);

        // The nav ListBox only speaks on keyboard arrow — not when selection is set by mouse click
        // or programmatically — so announce the destination ourselves to confirm the page changed.
        if (_navAnnouncementsArmed)
        {
            _announcer.Announce(value.Label);
        }

        _navAnnouncementsArmed = true;
    }

    partial void OnSelectedLanguageChanged(LocaleOption? value)
    {
        if (value is null || string.Equals(value.CultureName, _localizer.Current.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _localizer.SetCulture(value.CultureName);
        _languageStore.SaveCulture(value.CultureName);
    }

    /// <summary>Every page view-model, for the shell to pre-build its view while the app is idle.</summary>
    public IEnumerable<object> PageViewModels =>
        Enum.GetValues<NavDestination>().Select(ResolvePage);

    private object ResolvePage(NavDestination destination) => destination switch
    {
        NavDestination.OpenFile => _services.GetRequiredService<OpenFileViewModel>(),
        NavDestination.Overview => _services.GetRequiredService<OverviewViewModel>(),
        NavDestination.Inventories => _services.GetRequiredService<InventoriesViewModel>(),
        NavDestination.TerraTokens => _services.GetRequiredService<TerraTokensViewModel>(),
        NavDestination.Teleport => _services.GetRequiredService<TeleportViewModel>(),
        NavDestination.About => _services.GetRequiredService<AboutViewModel>(),
        _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null),
    };

    private void Show(NavDestination destination, bool force = false)
    {
        var vm = ResolvePage(destination);
        PageError = null;

        if (vm is ILoadable loadable &&
            (force
             || !_loadedAtRevision.TryGetValue(destination, out var loadedRevision)
             || loadedRevision != _workspaceRevision))
        {
            try
            {
                loadable.Load();
                _loadedAtRevision[destination] = _workspaceRevision;
            }
            catch (Exception ex)
            {
                // A page that cannot be built must not take the app down with it. The file is
                // already open by this point and any unsaved edits would go too, which is what
                // made a single malformed field so expensive (issue #37).
                //
                // The revision is deliberately not recorded, so navigating back here tries again
                // rather than showing a stale error forever.
                System.Diagnostics.Trace.WriteLine($"Could not load the {destination} page: {ex}");
                PageError = _localizer[LocKeys.Shell_PageLoadFailed];
                _announcer.Announce(_localizer.Format(LocKeys.Announce_ErrorPrefix, PageError));
            }
        }

        CurrentPage = vm;
    }

    private void RefreshLabels()
    {
        foreach (var item in NavItems)
        {
            item.Label = _localizer[item.Destination switch
            {
                NavDestination.OpenFile => LocKeys.Nav_OpenFile,
                NavDestination.Overview => LocKeys.Nav_Overview,
                NavDestination.Inventories => LocKeys.Nav_Inventories,
                NavDestination.TerraTokens => LocKeys.Nav_TerraTokens,
                NavDestination.Teleport => LocKeys.Nav_Teleport,
                NavDestination.About => LocKeys.Nav_About,
                _ => string.Empty,
            }];
        }
    }

    private LocaleOption? MatchLanguage() =>
        Languages.FirstOrDefault(l => string.Equals(l.CultureName, _localizer.Current.Name, StringComparison.OrdinalIgnoreCase))
        ?? Languages.FirstOrDefault(l => string.Equals(
            CultureInfo.GetCultureInfo(l.CultureName).TwoLetterISOLanguageName,
            _localizer.Current.TwoLetterISOLanguageName,
            StringComparison.OrdinalIgnoreCase));
}
