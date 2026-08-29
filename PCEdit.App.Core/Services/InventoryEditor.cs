using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public sealed class InventoryEditor(ISaveFileWorkspace workspace, IItemCatalog itemCatalog, ILocalizer localizer) : IInventoryEditor
{
    private readonly ISaveFileWorkspace _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    private readonly IItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
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
                return new InventoryGroup
                {
                    InventoryId = inventory.Id,
                    Label = label,
                    Kind = kind,
                    Size = inventory.Size,
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
