using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

public interface IInventoryEditor
{
    List<InventoryGroup> BuildInventoryGroups();

    List<InventoryOptionView> GetDestinationOptions(int worldObjectId);

    MoveItemResult TryMoveItem(int worldObjectId, int destinationInventoryId);

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
