using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

public sealed partial class InventoriesViewModel(ISaveFileWorkspace workspace, IInventoryEditor inventoryEditor, INavigationService navigation) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IInventoryEditor _inventoryEditor = inventoryEditor;
    private readonly INavigationService _navigation = navigation;

    // A real save has hundreds of inventories: build the whole list once, then filter it in memory.
    private IReadOnlyList<InventoryGroup> _allGroups = [];

    public bool IsLoaded => _workspace.IsLoaded;

    [ObservableProperty]
    private IReadOnlyList<InventoryGroup> _groups = [];

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private InventoryFilter _filter = InventoryFilter.All;

    /// <summary>True when a file is loaded and has inventories, but the current query/filter hides
    /// them all — so the view can distinguish "no results" from "nothing loaded".</summary>
    public bool IsFilteredEmpty => _allGroups.Count > 0 && Groups.Count == 0;

    public void Load()
    {
        OnPropertyChanged(nameof(IsLoaded));
        _allGroups = _workspace.IsLoaded ? _inventoryEditor.BuildInventoryGroups() : [];
        ApplyFilter();
    }

    partial void OnQueryChanged(string value) => ApplyFilter();

    partial void OnFilterChanged(InventoryFilter value) => ApplyFilter();

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

        Groups = _allGroups
            .Where(g => (kind is null || g.Kind == kind) && g.Matches(term))
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
