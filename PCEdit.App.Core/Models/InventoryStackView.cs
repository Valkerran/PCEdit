namespace PCEdit.App.Core.Models;

/// <summary>
/// One row on an Inventories card: every item in the inventory that shares a stack key
/// (<see cref="InventoryItemView.StackKey"/>), in the save's order. A stacking-mod save can hold
/// thousands of one item in a chest, and a row per item froze the page (issue #64).
/// </summary>
public sealed record InventoryStackView(string GId, string DisplayName, string IconFile, IReadOnlyList<int> WorldObjectIds)
{
    public int Count => WorldObjectIds.Count;

    /// <summary>A one-item row shows that item's id; a larger stack shows its count instead.</summary>
    public bool IsSingle => Count == 1;

    public int FirstWorldObjectId => WorldObjectIds[0];

    /// <summary>The item a Move takes: the last of the stack, as the game takes overflow from the end.</summary>
    public int LastWorldObjectId => WorldObjectIds[^1];
}
