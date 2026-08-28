using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

public interface IInventoryEditor
{
    List<InventoryGroup> BuildInventoryGroups();

    List<InventoryOptionView> GetDestinationOptions(int worldObjectId);

    MoveItemResult TryMoveItem(int worldObjectId, int destinationInventoryId);
}
