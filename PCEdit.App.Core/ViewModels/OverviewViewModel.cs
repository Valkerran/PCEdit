using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class OverviewViewModel(
    ISaveFileWorkspace workspace,
    IScreenReaderAnnouncer announcer,
    ILocalizer localizer,
    INavigationService navigation) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly ILocalizer _localizer = localizer;
    private readonly INavigationService _navigation = navigation;

    public bool IsLoaded => _workspace.IsLoaded;

    /// <summary>
    /// The game version that wrote the loaded save (<c>metadata.version</c>), pre-formatted for
    /// display. Informational only — PCEdit does not gate editing on it.
    /// </summary>
    [ObservableProperty]
    private string? _gameVersionText;

    public ObservableCollection<PlayerOverviewRow> Players { get; } = [];

    public ObservableCollection<PlanetTerraformViewModel> Terraforms { get; } = [];

    public void Load()
    {
        Players.Clear();
        Terraforms.Clear();
        OnPropertyChanged(nameof(IsLoaded));
        GameVersionText = null;

        var save = _workspace.Current;
        if (save is null)
        {
            return;
        }

        GameVersionText = _localizer.Format(LocKeys.Overview_GameVersion, save.Metadata.Version);

        foreach (var player in save.Players)
        {
            Players.Add(new PlayerOverviewRow(
                player.Name,
                player.Host,
                _localizer.Format(LocKeys.Overview_PlayerLocation, player.PlanetId, player.PlayerPosition),
                _localizer.Format(
                    LocKeys.Overview_PlayerProgress,
                    player.TotalCraftedObjects.ToString(CultureInfo.CurrentCulture),
                    player.TotalTerraTokenEarned.ToString(CultureInfo.CurrentCulture)),
                player.PlayerGaugeOxygen,
                player.PlayerGaugeThirst,
                player.PlayerGaugeHealth,
                player.PlayerGaugeToxic));
        }

        foreach (var terraformation in save.Terraformations)
        {
            Terraforms.Add(new PlanetTerraformViewModel(_workspace, _announcer, _localizer, terraformation));
        }

        if (Terraforms.Count > 0)
        {
            Terraforms[0].IsExpanded = true;
        }
    }

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();
}
