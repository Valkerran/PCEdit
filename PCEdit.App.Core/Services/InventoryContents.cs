using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// What an inventory holds, counted one way everywhere: the card, the Move dialog, the move's
/// room check and the repair (issue #90). An id that names no world object loads as nothing, and
/// an id listed twice is still one item, so neither takes a slot.
/// </summary>
internal static class InventoryContents
{
    /// <summary>The ids naming an existing world object, each once, in the inventory's order.</summary>
    public static List<int> HeldIds(Inventory inventory, IReadOnlyDictionary<int, WorldObject> worldObjects) =>
        WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(worldObjects.ContainsKey).Distinct().ToList();
}
