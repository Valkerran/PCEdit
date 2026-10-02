using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

public interface IInventoryEditor
{
    List<InventoryGroup> BuildInventoryGroups();

    List<InventoryOptionView> GetDestinationOptions(int worldObjectId);

    MoveItemResult TryMoveItem(int worldObjectId, int destinationInventoryId);

    /// <summary>
    /// Moves several items, all from one inventory, into another - all of them or none. Fails
    /// without changing anything when they are not in a single inventory or the destination has
    /// no room for every one. Moving out of an over-full inventory is always allowed.
    /// </summary>
    MoveItemResult TryMoveItems(IReadOnlyList<int> worldObjectIds, int destinationInventoryId);

    /// <summary>
    /// The current logistics config for one inventory, or null if it is not a logistics container.
    /// </summary>
    LogisticsContainerView? GetLogisticsContainer(int inventoryId);

    /// <summary>
    /// Replaces a logistics container's demand groups, supply groups and priority. Throws if the
    /// inventory is not a logistics container (only those carry a <c>priority</c> in the save).
    /// </summary>
    void UpdateLogistics(int inventoryId, IReadOnlyList<string> demandGroupIds, IReadOnlyList<string> supplyGroupIds, int priority);
}
