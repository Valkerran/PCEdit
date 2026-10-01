using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class OverviewViewModel(
    ISaveFileWorkspace workspace,
    IScreenReaderAnnouncer announcer,
    ILocalizer localizer,
    INavigationService navigation,
    IInventoryEditor inventoryEditor) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly ILocalizer _localizer = localizer;
    private readonly INavigationService _navigation = navigation;
    private readonly IInventoryEditor _inventoryEditor = inventoryEditor;

    public bool IsLoaded => _workspace.IsLoaded;

    /// <summary>
    /// The game version that wrote the loaded save (<c>metadata.version</c>), pre-formatted for
    /// display. Informational only — PCEdit does not gate editing on it.
    /// </summary>
    [ObservableProperty]
    private string? _gameVersionText;

    /// <summary>
    /// "Containers holding more items than their size: N" - the over-full banner's first line
    /// (issue #64); null on a save with none, which hides the banner's line.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttention))]
    private string? _overCapacityText;

    /// <summary>The banner's second line: inventories near the game's per-inventory load cap.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttention))]
    private string? _nearLoadLimitText;

    public bool HasAttention => OverCapacityText is not null || NearLoadLimitText is not null;

    public ObservableCollection<PlayerOverviewRow> Players { get; } = [];

    public ObservableCollection<PlanetTerraformViewModel> Terraforms { get; } = [];

    public void Load()
    {
        Players.Clear();
        Terraforms.Clear();
        OnPropertyChanged(nameof(IsLoaded));
        GameVersionText = null;
        OverCapacityText = null;
        NearLoadLimitText = null;

        var save = _workspace.Current;
        if (save is null)
        {
            return;
        }

        GameVersionText = _localizer.Format(LocKeys.Overview_GameVersion, save.Metadata.Version);
        SummariseAttention();
        AddPlayers(save);
        AddTerraforms(save);
    }

    private void AddPlayers(PlanetCrafterSaveFile save)
    {
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
    }

    /// <summary>One accordion entry per planet, the first opened.</summary>
    private void AddTerraforms(PlanetCrafterSaveFile save)
    {
        foreach (var terraformation in save.Terraformations)
        {
            Terraforms.Add(new PlanetTerraformViewModel(_workspace, _announcer, _localizer, terraformation));
        }

        if (Terraforms.Count > 0)
        {
            Terraforms[0].IsExpanded = true;
        }
    }

    /// <summary>
    /// Counts from the same cards the Inventories page shows, so the banner and the badges can
    /// never disagree. Building every card just to count costs ~0.4 s on a 300k-object
    /// stacking-mod save and nothing noticeable on an unmodded one; count straight from the save
    /// instead if that ever changes.
    /// </summary>
    private void SummariseAttention()
    {
        var groups = _inventoryEditor.BuildInventoryGroups();
        var overCapacity = groups.Count(g => g.IsOverCapacity);
        var nearLoadLimit = groups.Count(g => g.IsNearLoadLimit);
        OverCapacityText = overCapacity > 0
            ? _localizer.Format(LocKeys.Overview_AttentionOverCapacity, overCapacity.ToString(CultureInfo.CurrentCulture))
            : null;
        NearLoadLimitText = nearLoadLimit > 0
            ? _localizer.Format(LocKeys.Overview_AttentionNearLoadLimit, nearLoadLimit.ToString(CultureInfo.CurrentCulture))
            : null;
    }

    [RelayCommand]
    private Task ShowAttention() => _navigation.GoToInventoriesNeedingAttentionAsync();

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();
}
