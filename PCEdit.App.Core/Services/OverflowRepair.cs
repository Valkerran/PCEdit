using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Trims over-full inventories back to what a save can safely hold (issue #64): at most
/// <c>multiple</c> times each inventory's size, and never past the near-load-limit line. At a
/// multiple of 1 the save holds nothing an unmodded game could not have built.
/// </summary>
/// <remarks>
/// Before anything is removed, the item types the player ticks (<c>moveFirst</c>, in priority
/// order) move into free slots in their own storage crates - see <see cref="FreeStorage"/>. The
/// first of each inventory's excess moves; what does not fit is the last of the list, as the game
/// takes overflow from the end, and is removed with its world-object record. Two kinds of item are
/// never touched: one that owns an inventory of its own (removing it would orphan that inventory),
/// and an entry PCEdit cannot read (issue #37). A record another inventory still lists is kept.
/// </remarks>
public sealed class OverflowRepair(ISaveFileWorkspace workspace, IItemCatalog itemCatalog) : IOverflowRepair
{
    private readonly ISaveFileWorkspace _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    private readonly IItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

    public RepairPlan Plan(int multiple, IReadOnlyList<string>? moveFirst = null) =>
        Summarise(Decide(multiple, moveFirst ?? []));

    public RepairPlan Apply(int multiple, IReadOnlyList<string>? moveFirst = null)
    {
        var decision = Decide(multiple, moveFirst ?? []);
        foreach (var (inventoryId, leaving) in decision.Leaving)
        {
            _workspace.ReplaceInventory(inventoryId, inventory => Rewrite(inventory,
                WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(id => !leaving.Contains(id))));
        }

        foreach (var (inventoryId, arriving) in decision.Storage.Received)
        {
            _workspace.ReplaceInventory(inventoryId, inventory => Rewrite(inventory,
                WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Concat(arriving)));
        }

        _workspace.RemoveWorldObjects(RecordsNoLongerListed(decision.Removed));
        return Summarise(decision);
    }

    /// <summary>What a repair does: the excess of each inventory (in its own order), which of it
    /// moves into free storage, and which is removed.</summary>
    private sealed record Decision(
        IReadOnlyList<(int InventoryId, List<int> Excess)> Excess,
        IReadOnlyList<(int InventoryId, HashSet<int> Leaving)> Leaving,
        IReadOnlyList<int> Removed,
        int MovedCount,
        FreeStorage Storage,
        IReadOnlyDictionary<int, WorldObject> WorldObjects);

    private Decision Decide(int multiple, IReadOnlyList<string> moveFirst)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(multiple, 1);
        var save = _workspace.Current ?? throw new InvalidOperationException("No save file is loaded.");
        var worldObjects = new Dictionary<int, WorldObject>();
        foreach (var worldObject in save.WorldObjects)
        {
            worldObjects.TryAdd(worldObject.Id, worldObject);
        }

        var excess = save.Inventories
            .Select(inventory => (inventory.Id, Excess: PickExcess(inventory, multiple, worldObjects)))
            .Where(e => e.Excess.Count > 0)
            .ToList();

        var storage = FreeStorage.Find(save, worldObjects);
        var moved = PlaceTickedTypes(excess, moveFirst, storage, worldObjects);
        var removed = excess.SelectMany(e => e.Excess).Where(id => !moved.Contains(id)).ToList();
        var leaving = excess.Select(e => (e.Id, e.Excess.ToHashSet())).ToList();
        return new Decision(excess, leaving, removed, moved.Count, storage, worldObjects);
    }

    /// <summary>Places the ticked types into free storage, in the player's order: each type's
    /// excess, inventory by inventory, until the room on that planet runs out.</summary>
    private static HashSet<int> PlaceTickedTypes(
        IReadOnlyList<(int InventoryId, List<int> Excess)> excess,
        IReadOnlyList<string> moveFirst,
        FreeStorage storage,
        IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        var byType = GroupByType(excess, worldObjects);
        var moved = new HashSet<int>();
        foreach (var gId in moveFirst.Distinct(StringComparer.Ordinal))
        {
            foreach (var (inventoryId, id) in byType.GetValueOrDefault(gId) ?? [])
            {
                if (storage.TryPlace(inventoryId, id, gId))
                {
                    moved.Add(id);
                }
            }
        }

        return moved;
    }

    /// <summary>
    /// The excess grouped by item type, keeping inventory and list order. Grouped once: rescanning
    /// every excess item for each ticked type took seconds on a stacking-mod save with 100+ types.
    /// </summary>
    private static Dictionary<string, List<(int InventoryId, int Id)>> GroupByType(
        IReadOnlyList<(int InventoryId, List<int> Excess)> excess,
        IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        var byType = new Dictionary<string, List<(int InventoryId, int Id)>>(StringComparer.Ordinal);
        foreach (var (inventoryId, items) in excess)
        {
            foreach (var id in items)
            {
                var gId = worldObjects[id].GId;
                if (!byType.TryGetValue(gId, out var ofType))
                {
                    byType[gId] = ofType = [];
                }

                ofType.Add((inventoryId, id));
            }
        }

        return byType;
    }

    /// <summary>The items over one inventory's limit, in the inventory's own order - the last
    /// ones, skipping any that own an inventory - or none if it is within its limit.</summary>
    private static List<int> PickExcess(Inventory inventory, int multiple, IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        // Counted as the Inventories page counts: ids that name an existing item.
        var items = WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(worldObjects.ContainsKey).ToList();
        var limit = (int)Math.Min((long)inventory.Size * multiple, InventoryGroup.NearLoadLimitThreshold);
        var over = items.Count - limit;
        var picked = new List<int>();
        for (var i = items.Count - 1; i >= 0 && picked.Count < over; i--)
        {
            if (!OwnsAnInventory(worldObjects[items[i]]))
            {
                picked.Add(items[i]);
            }
        }

        picked.Reverse();
        return picked;
    }

    private static bool OwnsAnInventory(WorldObject item) =>
        item.LinkedInventoryId is not null || item.SpawnedInstanceIds is not null;

    private static Inventory Rewrite(Inventory inventory, IEnumerable<int> ids) =>
        inventory with { WorldObjectIds = WorldObjectIdsCodec.Rewrite(inventory.WorldObjectIds, ids) };

    private IReadOnlySet<int> RecordsNoLongerListed(IReadOnlyList<int> removed)
    {
        var stillListed = _workspace.Current!.Inventories
            .SelectMany(inventory => WorldObjectIdsCodec.Parse(inventory.WorldObjectIds))
            .ToHashSet();
        return removed.Where(id => !stillListed.Contains(id)).ToHashSet();
    }

    private RepairPlan Summarise(Decision decision)
    {
        var byType = decision.Removed
            .GroupBy(id => DisplayName(decision.WorldObjects[id].GId))
            .Select(g => new RemovedItemCount(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.DisplayName, StringComparer.CurrentCulture)
            .ToList();
        var excessByType = decision.Excess
            .SelectMany(e => e.Excess)
            .GroupBy(id => decision.WorldObjects[id].GId, StringComparer.Ordinal)
            .Select(g => new ExcessType(g.Key, DisplayName(g.Key), g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.DisplayName, StringComparer.CurrentCulture)
            .ToList();
        return new RepairPlan(decision.Removed.Count, decision.Excess.Count, byType)
        {
            MovedCount = decision.MovedCount,
            DestinationCount = decision.Storage.Received.Count(),
            FreeSlots = decision.Storage.FreeSlots,
            ExcessByType = excessByType,
        };
    }

    private string DisplayName(string gId) => _itemCatalog.Resolve(gId).DisplayName;
}
