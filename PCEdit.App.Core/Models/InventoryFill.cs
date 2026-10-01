namespace PCEdit.App.Core.Models;

/// <summary>
/// How full an inventory is, measured against what an unmodded game produces (issue #64).
/// </summary>
public enum InventoryFill
{
    /// <summary>No more items than slots.</summary>
    Normal,

    /// <summary>
    /// More items than slots. The unmodded game never builds this; an inventory-stacking mod
    /// leaves exactly this behind, since it keeps <c>size</c> and lists every stacked item.
    /// </summary>
    OverFull,

    /// <summary>
    /// Close to the game's per-inventory load cap, past which the game silently drops items on
    /// load. Takes precedence over <see cref="OverFull"/>.
    /// </summary>
    NearLoadLimit,
}
