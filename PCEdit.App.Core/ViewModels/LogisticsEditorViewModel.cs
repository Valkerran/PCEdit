using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// Modal editor for one logistics container's demand groups, supply groups and priority. A group
/// is added either by picking a known one (<see cref="AllGroups"/>) or by typing a raw id — the
/// bundled pick-list is not exhaustive.
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
    private string _priority = "0";

    [ObservableProperty]
    private LogisticsGroupInfo? _demandGroupToAdd;

    [ObservableProperty]
    private string _demandGroupText = string.Empty;

    [ObservableProperty]
    private LogisticsGroupInfo? _supplyGroupToAdd;

    [ObservableProperty]
    private string _supplyGroupText = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    /// <summary>Every known group, for the "add" pickers.</summary>
    public IReadOnlyList<LogisticsGroupInfo> AllGroups => _groupCatalog.All;

    public ObservableCollection<LogisticsGroupInfo> DemandGroups { get; } = [];

    public ObservableCollection<LogisticsGroupInfo> SupplyGroups { get; } = [];

    public void Initialize(int inventoryId)
    {
        var container = _inventoryEditor.GetLogisticsContainer(inventoryId)
            ?? throw new InvalidOperationException($"Inventory {inventoryId} is not a logistics container.");

        InventoryId = inventoryId;
        Title = container.Label;
        Priority = container.Priority.ToString(CultureInfo.InvariantCulture);

        DemandGroups.Clear();
        foreach (var id in container.DemandGroupIds)
        {
            DemandGroups.Add(_groupCatalog.Resolve(id));
        }

        SupplyGroups.Clear();
        foreach (var id in container.SupplyGroupIds)
        {
            SupplyGroups.Add(_groupCatalog.Resolve(id));
        }
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
    }

    [RelayCommand]
    private void RemoveDemandGroup(LogisticsGroupInfo group) => DemandGroups.Remove(group);

    [RelayCommand]
    private void AddSupplyGroup()
    {
        if (TryResolveGroup(SupplyGroupToAdd, SupplyGroupText, out var group))
        {
            AddUnique(SupplyGroups, group);
        }

        SupplyGroupToAdd = null;
        SupplyGroupText = string.Empty;
    }

    [RelayCommand]
    private void RemoveSupplyGroup(LogisticsGroupInfo group) => SupplyGroups.Remove(group);

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (!int.TryParse(Priority, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority) || priority < 0)
        {
            StatusKind = StatusKind.Error;
            StatusMessage = _localizer[LocKeys.Logistics_InvalidPriority];
            _announcer.Announce(_localizer.Format(LocKeys.Announce_ErrorPrefix, StatusMessage));
            return;
        }

        _inventoryEditor.UpdateLogistics(
            InventoryId,
            DemandGroups.Select(g => g.Id).ToList(),
            SupplyGroups.Select(g => g.Id).ToList(),
            priority);

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

    private static void AddUnique(ObservableCollection<LogisticsGroupInfo> target, LogisticsGroupInfo group)
    {
        if (!target.Any(g => string.Equals(g.Id, group.Id, StringComparison.Ordinal)))
        {
            target.Add(group);
        }
    }
}
