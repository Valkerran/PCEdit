using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public sealed class InventoryEditor(
    ISaveFileWorkspace workspace,
    IItemCatalog itemCatalog,
    ILogisticsGroupCatalog logisticsGroupCatalog,
    ILocalizer localizer) : IInventoryEditor
{
    private readonly ISaveFileWorkspace _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    private readonly IItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
    private readonly ILogisticsGroupCatalog _logisticsGroupCatalog = logisticsGroupCatalog ?? throw new ArgumentNullException(nameof(logisticsGroupCatalog));
    private readonly ILocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    public List<InventoryGroup> BuildInventoryGroups()
    {
        var save = RequireCurrent();
        var worldObjectsById = save.WorldObjects.ToDictionary(w => w.Id);
        var containersByInventoryId = BuildContainerLookup(save);

        return save.Inventories
            .Select(inventory =>
            {
                var (label, kind) = DescribeInventory(save, inventory.Id, containersByInventoryId);
                var logistics = ReadLogistics(inventory);
                return new InventoryGroup
                {
                    InventoryId = inventory.Id,
                    Label = label,
                    Kind = kind,
                    Size = inventory.Size,
                    Logistics = logistics,
                    LogisticsSummary = logistics is null ? null : FormatLogisticsSummary(logistics),
                    Items = WorldObjectIdsCodec.Parse(inventory.WorldObjectIds)
                        .Where(worldObjectsById.ContainsKey)
                        .Select(id => ToItemView(worldObjectsById[id], inventory.Id))
                        .ToList()
                };
            })
            .ToList();
    }

    /// <summary>
    /// Index of the storage object that owns each linked inventory. Built once per call so
    /// <see cref="DescribeInventory"/> is O(1) per inventory rather than scanning every world object
    /// (a real save has thousands of world objects and hundreds of inventories).
    /// </summary>
    private static Dictionary<int, WorldObject> BuildContainerLookup(PlanetCrafterSaveFile save)
    {
        var lookup = new Dictionary<int, WorldObject>();
        foreach (var worldObject in save.WorldObjects)
        {
            if (worldObject.LinkedInventoryId is { } inventoryId)
            {
                lookup.TryAdd(inventoryId, worldObject);
            }
        }

        return lookup;
    }

    private InventoryItemView ToItemView(WorldObject worldObject, int inventoryId)
    {
        var info = _itemCatalog.Resolve(worldObject.GId);
        return new InventoryItemView(worldObject.Id, worldObject.GId, inventoryId, info.DisplayName, info.IconFile);
    }

    public List<InventoryOptionView> GetDestinationOptions(int worldObjectId)
    {
        var save = RequireCurrent();
        var sourceInventoryId = FindOwningInventory(save, worldObjectId)?.Id;
        var containersByInventoryId = BuildContainerLookup(save);

        return save.Inventories
            .Where(inventory => inventory.Id != sourceInventoryId)
            .Select(inventory => new InventoryOptionView(
                inventory.Id,
                DescribeInventory(save, inventory.Id, containersByInventoryId).Label,
                WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Count,
                inventory.Size))
            .ToList();
    }

    public MoveItemResult TryMoveItem(int worldObjectId, int destinationInventoryId)
    {
        var save = RequireCurrent();

        var source = FindOwningInventory(save, worldObjectId);
        if (source is null)
        {
            return MoveItemResult.Fail(_localizer[LocKeys.Inv_NotInInventory]);
        }

        if (source.Id == destinationInventoryId)
        {
            return MoveItemResult.Fail(_localizer[LocKeys.Inv_AlreadyThere]);
        }

        var destination = save.Inventories.FirstOrDefault(i => i.Id == destinationInventoryId);
        if (destination is null)
        {
            return MoveItemResult.Fail(_localizer[LocKeys.Inv_DestNotFound]);
        }

        var destinationIds = WorldObjectIdsCodec.Parse(destination.WorldObjectIds);
        if (destinationIds.Count >= destination.Size)
        {
            return MoveItemResult.Fail(_localizer.Format(LocKeys.Inv_DestFull, destinationIds.Count, destination.Size));
        }

        _workspace.ReplaceInventory(source.Id, inventory => WithWorldObjectIds(
            inventory,
            WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(id => id != worldObjectId)));

        _workspace.ReplaceInventory(destination.Id, inventory => WithWorldObjectIds(
            inventory,
            WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Append(worldObjectId)));

        return MoveItemResult.Ok();
    }

    public LogisticsContainerView? GetLogisticsContainer(int inventoryId)
    {
        var save = RequireCurrent();
        var inventory = save.Inventories.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory?.Priority is not { } priority)
        {
            return null;
        }

        var containersByInventoryId = BuildContainerLookup(save);
        var (label, _) = DescribeInventory(save, inventoryId, containersByInventoryId);
        return new LogisticsContainerView(
            inventoryId,
            label,
            GroupListCodec.Parse(inventory.DemandGroups),
            GroupListCodec.Parse(inventory.SupplyGroups),
            priority);
    }

    public void UpdateLogistics(int inventoryId, IReadOnlyList<string> demandGroupIds, IReadOnlyList<string> supplyGroupIds, int priority)
    {
        ArgumentNullException.ThrowIfNull(demandGroupIds);
        ArgumentNullException.ThrowIfNull(supplyGroupIds);

        _workspace.ReplaceInventory(inventoryId, inventory =>
        {
            if (inventory.Priority is null)
            {
                throw new InvalidOperationException($"Inventory {inventoryId} is not a logistics container.");
            }

            return inventory with
            {
                DemandGroups = GroupListCodec.Join(demandGroupIds),
                SupplyGroups = GroupListCodec.Join(supplyGroupIds),
                Priority = priority
            };
        });
    }

    private LogisticsConfig? ReadLogistics(Inventory inventory)
    {
        return inventory.Priority is { } priority
            ? new LogisticsConfig(
                GroupListCodec.Parse(inventory.DemandGroups),
                GroupListCodec.Parse(inventory.SupplyGroups),
                priority)
            : null;
    }

    private string FormatLogisticsSummary(LogisticsConfig logistics)
    {
        return _localizer.Format(
            LocKeys.Inventories_LogisticsSummary,
            DescribeGroupCount(logistics.DemandGroupIds),
            DescribeGroupCount(logistics.SupplyGroupIds),
            DescribePriority(logistics.Priority));
    }

    /// <summary>A named level's friendly name, or "Unknown (N)" for a raw value outside -3..3.</summary>
    private string DescribePriority(int raw)
    {
        return LogisticsPriorityLevels.Known(raw) is { } level
            ? _localizer[level.ResourceKey()]
            : _localizer.Format(LocKeys.Logistics_PriorityUnknown, raw);
    }

    /// <summary>A count, or "Everything" when the list holds every known group.</summary>
    private string DescribeGroupCount(IReadOnlyCollection<string> groupIds)
    {
        var known = _logisticsGroupCatalog.All;
        if (known.Count > 0 && known.All(g => groupIds.Contains(g.Id)))
        {
            return _localizer[LocKeys.Logistics_Everything];
        }

        return groupIds.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
    }

    private static Inventory WithWorldObjectIds(Inventory inventory, IEnumerable<int> ids)
    {
        return inventory with { WorldObjectIds = WorldObjectIdsCodec.Join(ids) };
    }

    private static Inventory? FindOwningInventory(PlanetCrafterSaveFile save, int worldObjectId)
    {
        return save.Inventories.FirstOrDefault(i => WorldObjectIdsCodec.Parse(i.WorldObjectIds).Contains(worldObjectId));
    }

    private (string Label, InventoryKind Kind) DescribeInventory(
        PlanetCrafterSaveFile save,
        int inventoryId,
        IReadOnlyDictionary<int, WorldObject> containersByInventoryId)
    {
        var owningPlayer = save.Players.FirstOrDefault(p => p.InventoryId == inventoryId);
        if (owningPlayer is not null)
        {
            return (_localizer.Format(LocKeys.Inv_PlayerInventory, owningPlayer.Name), InventoryKind.PlayerInventory);
        }

        var equippingPlayer = save.Players.FirstOrDefault(p => p.EquipmentId == inventoryId);
        if (equippingPlayer is not null)
        {
            return (_localizer.Format(LocKeys.Inv_PlayerEquipment, equippingPlayer.Name), InventoryKind.Equipment);
        }

        if (containersByInventoryId.TryGetValue(inventoryId, out var container))
        {
            return (_localizer.Format(LocKeys.Inv_Container, _itemCatalog.Resolve(container.GId).DisplayName, container.Id), InventoryKind.Container);
        }

        return (_localizer.Format(LocKeys.Inv_Fallback, inventoryId), InventoryKind.Other);
    }

    private PlanetCrafterSaveFile RequireCurrent()
    {
        return _workspace.Current ?? throw new InvalidOperationException("No save file is loaded.");
    }
}
