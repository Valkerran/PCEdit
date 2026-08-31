using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

/// <summary>Inventories page type filter.</summary>
public enum InventoryFilter
{
    All,
    Players,
    Equipment,
    Containers,
}

public sealed partial class InventoriesViewModel(
    ISaveFileWorkspace workspace,
    IInventoryEditor inventoryEditor,
    INavigationService navigation,
    ILocalizer localizer) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IInventoryEditor _inventoryEditor = inventoryEditor;
    private readonly INavigationService _navigation = navigation;
    private readonly ILocalizer _localizer = localizer;

    // A real save has hundreds of inventories: build the whole list once, then filter it in memory.
    private IReadOnlyList<InventoryGroup> _allGroups = [];

    public bool IsLoaded => _workspace.IsLoaded;

    [ObservableProperty]
    private IReadOnlyList<InventoryGroup> _groups = [];

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private InventoryFilter _filter = InventoryFilter.All;

    /// <summary>The "filter by world" options: "All worlds", then one per world present in the
    /// save, then "Unknown world" if any inventory has no resolvable world.</summary>
    public ObservableCollection<WorldFilterOption> WorldOptions { get; } = [];

    [ObservableProperty]
    private WorldFilterOption? _selectedWorld;

    /// <summary>Only worth showing the world filter when the save actually spans more than one.</summary>
    public bool ShowWorldFilter => WorldOptions.Count(o => o.PlanetId is not null) > 1;

    /// <summary>True when a file is loaded and has inventories, but the current query/filter hides
    /// them all — so the view can distinguish "no results" from "nothing loaded".</summary>
    public bool IsFilteredEmpty => _allGroups.Count > 0 && Groups.Count == 0;

    public void Load()
    {
        OnPropertyChanged(nameof(IsLoaded));
        _allGroups = _workspace.IsLoaded ? _inventoryEditor.BuildInventoryGroups() : [];
        RebuildWorldOptions();
        ApplyFilter();
    }

    partial void OnQueryChanged(string value) => ApplyFilter();

    partial void OnFilterChanged(InventoryFilter value) => ApplyFilter();

    partial void OnSelectedWorldChanged(WorldFilterOption? value) => ApplyFilter();

    private void RebuildWorldOptions()
    {
        WorldOptions.Clear();
        WorldOptions.Add(WorldFilterOption.All(_localizer[LocKeys.Inventories_WorldAll]));

        foreach (var planetId in _allGroups
                     .Select(g => g.PlanetId)
                     .Where(id => id is not null)
                     .Select(id => id!)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            WorldOptions.Add(WorldFilterOption.ForPlanet(planetId));
        }

        if (_allGroups.Any(g => g.PlanetId is null))
        {
            WorldOptions.Add(WorldFilterOption.Unknown(_localizer[LocKeys.Inventories_WorldUnknown]));
        }

        SelectedWorld = WorldOptions[0];
        OnPropertyChanged(nameof(ShowWorldFilter));
    }

    private void ApplyFilter()
    {
        var term = Query.Trim().ToLowerInvariant();
        var kind = Filter switch
        {
            InventoryFilter.Players => (InventoryKind?)InventoryKind.PlayerInventory,
            InventoryFilter.Equipment => InventoryKind.Equipment,
            InventoryFilter.Containers => InventoryKind.Container,
            _ => null,
        };
        var world = SelectedWorld;

        Groups = _allGroups
            .Where(g => (kind is null || g.Kind == kind)
                        && (world is null || world.Accepts(g.PlanetId))
                        && g.Matches(term))
            .ToList();
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    [RelayCommand]
    private async Task MoveItemAsync(InventoryItemView item)
    {
        await _navigation.OpenSelectInventoryAsync(item.WorldObjectId);
    }

    [RelayCommand]
    private async Task EditLogisticsAsync(InventoryGroup group)
    {
        await _navigation.OpenLogisticsEditorAsync(group.InventoryId);
    }

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();
}
