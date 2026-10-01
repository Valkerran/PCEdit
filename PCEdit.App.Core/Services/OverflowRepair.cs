using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Trims over-full inventories back to what a save can safely hold (issue #64): at most
/// <c>multiple</c> times each inventory's size, and never past the near-load-limit line. At a
/// multiple of 1 the save holds nothing an unmodded game could not have built.
/// </summary>
/// <remarks>
/// The items removed are the last of each list, as the game takes overflow from the end, and
/// their world-object records go with them. Two kinds of item are never removed: one that owns an
/// inventory of its own (deleting it would orphan that inventory), and an entry PCEdit cannot read
/// (issue #37). A record another inventory still lists is kept.
/// </remarks>
public sealed class OverflowRepair(ISaveFileWorkspace workspace, IItemCatalog itemCatalog) : IOverflowRepair
{
    private readonly ISaveFileWorkspace _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    private readonly IItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

    public RepairPlan Plan(int multiple) => Summarise(FindExcess(multiple));

    public RepairPlan Apply(int multiple)
    {
        var excess = FindExcess(multiple);
        foreach (var (inventoryId, removed) in excess.Removals)
        {
            var leaving = removed.ToHashSet();
            _workspace.ReplaceInventory(inventoryId, inventory => inventory with
            {
                WorldObjectIds = WorldObjectIdsCodec.Rewrite(
                    inventory.WorldObjectIds,
                    WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(id => !leaving.Contains(id))),
            });
        }

        _workspace.RemoveWorldObjects(RecordsNoLongerListed(excess));
        return Summarise(excess);
    }

    private sealed record Excess(IReadOnlyList<(int InventoryId, List<int> Removed)> Removals, IReadOnlyDictionary<int, WorldObject> WorldObjects);

    private Excess FindExcess(int multiple)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(multiple, 1);
        var save = _workspace.Current ?? throw new InvalidOperationException("No save file is loaded.");
        var worldObjects = new Dictionary<int, WorldObject>();
        foreach (var worldObject in save.WorldObjects)
        {
            worldObjects.TryAdd(worldObject.Id, worldObject);
        }

        var removals = new List<(int, List<int>)>();
        foreach (var inventory in save.Inventories)
        {
            var removed = PickRemovals(inventory, multiple, worldObjects);
            if (removed.Count > 0)
            {
                removals.Add((inventory.Id, removed));
            }
        }

        return new Excess(removals, worldObjects);
    }

    /// <summary>The items to take off the end of one inventory, or none if it is within its limit.</summary>
    private static List<int> PickRemovals(Inventory inventory, int multiple, IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        // Counted as the Inventories page counts: ids that name an existing item.
        var items = WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(worldObjects.ContainsKey).ToList();
        var limit = (int)Math.Min((long)inventory.Size * multiple, InventoryGroup.NearLoadLimitThreshold);
        var excess = items.Count - limit;
        var removed = new List<int>();
        for (var i = items.Count - 1; i >= 0 && removed.Count < excess; i--)
        {
            if (!OwnsAnInventory(worldObjects[items[i]]))
            {
                removed.Add(items[i]);
            }
        }

        return removed;
    }

    private static bool OwnsAnInventory(WorldObject item) =>
        item.LinkedInventoryId is not null || item.SpawnedInstanceIds is not null;

    private IReadOnlySet<int> RecordsNoLongerListed(Excess excess)
    {
        var stillListed = _workspace.Current!.Inventories
            .SelectMany(inventory => WorldObjectIdsCodec.Parse(inventory.WorldObjectIds))
            .ToHashSet();
        return excess.Removals.SelectMany(r => r.Removed).Where(id => !stillListed.Contains(id)).ToHashSet();
    }

    private RepairPlan Summarise(Excess excess)
    {
        var byType = excess.Removals
            .SelectMany(r => r.Removed)
            .GroupBy(id => _itemCatalog.Resolve(excess.WorldObjects[id].GId).DisplayName)
            .Select(g => new RemovedItemCount(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.DisplayName, StringComparer.CurrentCulture)
            .ToList();
        return new RepairPlan(byType.Sum(t => t.Count), excess.Removals.Count, byType);
    }
}
