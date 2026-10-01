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
    NeedsAttention,
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

    /// <summary>How many inventories, across the whole save, the "Needs attention" filter would
    /// show - for its chip label, so the problem is visible before the user filters for it.</summary>
    public int AttentionCount { get; private set; }

    public void Load()
    {
        OnPropertyChanged(nameof(IsLoaded));
        _allGroups = _workspace.IsLoaded ? _inventoryEditor.BuildInventoryGroups() : [];
        AttentionCount = _allGroups.Count(g => g.NeedsAttention);
        OnPropertyChanged(nameof(AttentionCount));
        if (AttentionCount == 0 && Filter == InventoryFilter.NeedsAttention)
        {
            // The view hides the chip when nothing is flagged; don't strand the user on it.
            Filter = InventoryFilter.All;
        }
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
        Func<InventoryGroup, bool> passesFilter = Filter switch
        {
            InventoryFilter.Players => g => g.Kind == InventoryKind.PlayerInventory,
            InventoryFilter.Equipment => g => g.Kind == InventoryKind.Equipment,
            InventoryFilter.Containers => g => g.Kind == InventoryKind.Container,
            InventoryFilter.NeedsAttention => g => g.NeedsAttention,
            _ => _ => true,
        };
        var world = SelectedWorld;

        // NarrowTo keeps a card whole when the inventory itself matches, and cuts it down to the
        // matching items when only its contents do - so a search for one item does not bury it
        // among everything else in its container. _allGroups itself is never modified.
        Groups = _allGroups
            .Where(g => passesFilter(g) && (world is null || world.Accepts(g.PlanetId)))
            .Select(g => g.NarrowTo(term))
            .OfType<InventoryGroup>()
            .ToList();
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    /// <summary>Moves one item of the stack: its last, as the game takes overflow from the end.</summary>
    [RelayCommand]
    private async Task MoveStackAsync(InventoryStackView stack)
    {
        await _navigation.OpenSelectInventoryAsync(stack.LastWorldObjectId);
    }

    [RelayCommand]
    private async Task EditLogisticsAsync(InventoryGroup group)
    {
        await _navigation.OpenLogisticsEditorAsync(group.InventoryId);
    }

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();
}
