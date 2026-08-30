using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// Modal editor for one logistics container's demand groups, supply groups and priority. A group
/// is added either by picking a known one (<see cref="AllGroups"/>) or by typing a raw id — the
/// bundled pick-list is not exhaustive. "Select all" adds every known group; when a list holds
/// every known group it collapses to a single "Everything" entry (matching the game).
/// </summary>
public sealed partial class LogisticsEditorViewModel(
    IInventoryEditor inventoryEditor,
    ILogisticsGroupCatalog groupCatalog,
    IScreenReaderAnnouncer announcer,
    INavigationService navigation,
    ILocalizer localizer) : ObservableObject
{
    private readonly IInventoryEditor _inventoryEditor = inventoryEditor;
    private readonly ILogisticsGroupCatalog _groupCatalog = groupCatalog;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly INavigationService _navigation = navigation;
    private readonly ILocalizer _localizer = localizer;

    [ObservableProperty]
    private int _inventoryId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private LogisticsPriorityChoice? _selectedPriority;

    [ObservableProperty]
    private LogisticsGroupInfo? _demandGroupToAdd;

    [ObservableProperty]
    private string _demandGroupText = string.Empty;

    [ObservableProperty]
    private LogisticsGroupInfo? _supplyGroupToAdd;

    [ObservableProperty]
    private string _supplyGroupText = string.Empty;

    /// <summary>Every known group, for the "add" pickers.</summary>
    public IReadOnlyList<LogisticsGroupInfo> AllGroups => _groupCatalog.All;

    /// <summary>
    /// The 7 named priority levels, lowest first. When the container's saved priority is outside
    /// -3..3 (a future game build), that raw value is prepended as an "Unknown (N)" choice so it
    /// survives an edit untouched.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LogisticsPriorityChoice> _priorityChoices = [];

    private LogisticsPriorityChoice NamedChoice(LogisticsPriority level) =>
        new(level.ToRaw(), level, _localizer[level.ResourceKey()]);

    public ObservableCollection<LogisticsGroupInfo> DemandGroups { get; } = [];

    public ObservableCollection<LogisticsGroupInfo> SupplyGroups { get; } = [];

    /// <summary>True when the demand list holds every known group — the view collapses it to "Everything".</summary>
    public bool DemandIsEverything => IsEverything(DemandGroups);

    public bool SupplyIsEverything => IsEverything(SupplyGroups);

    public void Initialize(int inventoryId)
    {
        var container = _inventoryEditor.GetLogisticsContainer(inventoryId)
            ?? throw new InvalidOperationException($"Inventory {inventoryId} is not a logistics container.");

        InventoryId = inventoryId;
        Title = container.Label;

        var choices = LogisticsPriorityLevels.All.Select(NamedChoice).ToList();
        if (LogisticsPriorityLevels.Known(container.Priority) is null)
        {
            choices.Insert(0, new LogisticsPriorityChoice(
                container.Priority,
                Level: null,
                _localizer.Format(LocKeys.Logistics_PriorityUnknown, container.Priority)));
        }

        PriorityChoices = choices;
        SelectedPriority = choices.First(c => c.RawValue == container.Priority);

        Replace(DemandGroups, container.DemandGroupIds);
        Replace(SupplyGroups, container.SupplyGroupIds);
    }

    [RelayCommand]
    private void AddDemandGroup()
    {
        if (TryResolveGroup(DemandGroupToAdd, DemandGroupText, out var group))
        {
            AddUnique(DemandGroups, group);
        }

        DemandGroupToAdd = null;
        DemandGroupText = string.Empty;
        NotifyListsChanged();
    }

    [RelayCommand]
    private void RemoveDemandGroup(LogisticsGroupInfo group)
    {
        DemandGroups.Remove(group);
        NotifyListsChanged();
    }

    [RelayCommand]
    private void SelectAllDemand()
    {
        Replace(DemandGroups, _groupCatalog.All.Select(g => g.Id));
    }

    [RelayCommand]
    private void ClearDemand()
    {
        DemandGroups.Clear();
        NotifyListsChanged();
    }

    [RelayCommand]
    private void AddSupplyGroup()
    {
        if (TryResolveGroup(SupplyGroupToAdd, SupplyGroupText, out var group))
        {
            AddUnique(SupplyGroups, group);
        }

        SupplyGroupToAdd = null;
        SupplyGroupText = string.Empty;
        NotifyListsChanged();
    }

    [RelayCommand]
    private void RemoveSupplyGroup(LogisticsGroupInfo group)
    {
        SupplyGroups.Remove(group);
        NotifyListsChanged();
    }

    [RelayCommand]
    private void SelectAllSupply()
    {
        Replace(SupplyGroups, _groupCatalog.All.Select(g => g.Id));
    }

    [RelayCommand]
    private void ClearSupply()
    {
        SupplyGroups.Clear();
        NotifyListsChanged();
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        _inventoryEditor.UpdateLogistics(
            InventoryId,
            DemandGroups.Select(g => g.Id).ToList(),
            SupplyGroups.Select(g => g.Id).ToList(),
            (SelectedPriority ?? PriorityChoices.First(c => c.Level == LogisticsPriority.Normal)).RawValue);

        _announcer.Announce(_localizer.Format(LocKeys.Logistics_Applied, Title));
        await _navigation.CloseModalAsync();
    }

    [RelayCommand]
    private async Task CancelAsync() => await _navigation.CloseModalAsync();

    private bool TryResolveGroup(LogisticsGroupInfo? picked, string typed, out LogisticsGroupInfo group)
    {
        if (picked is not null)
        {
            group = picked;
            return true;
        }

        var id = typed.Trim();
        if (id.Length > 0)
        {
            group = _groupCatalog.Resolve(id);
            return true;
        }

        group = null!;
        return false;
    }

    private bool IsEverything(IEnumerable<LogisticsGroupInfo> groups)
    {
        if (_groupCatalog.All.Count == 0)
        {
            return false;
        }

        var ids = groups.Select(g => g.Id).ToHashSet(StringComparer.Ordinal);
        return _groupCatalog.All.All(g => ids.Contains(g.Id));
    }

    private void Replace(ObservableCollection<LogisticsGroupInfo> target, IEnumerable<string> ids)
    {
        target.Clear();
        foreach (var id in ids)
        {
            AddUnique(target, _groupCatalog.Resolve(id));
        }

        NotifyListsChanged();
    }

    private void NotifyListsChanged()
    {
        OnPropertyChanged(nameof(DemandIsEverything));
        OnPropertyChanged(nameof(SupplyIsEverything));
    }

    private static void AddUnique(ObservableCollection<LogisticsGroupInfo> target, LogisticsGroupInfo group)
    {
        if (!target.Any(g => string.Equals(g.Id, group.Id, StringComparison.Ordinal)))
        {
            target.Add(group);
        }
    }
}
