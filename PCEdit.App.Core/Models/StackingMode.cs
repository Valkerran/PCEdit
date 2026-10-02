namespace PCEdit.App.Core.Models;

/// <summary>How the Inventories page shows identical items (issue #64).</summary>
public enum StackingMode
{
    /// <summary>One row per item, so ids show at a glance - grouped only when the save has
    /// containers that need attention, where a row per item would freeze the page. The default,
    /// and what a missing or unreadable stored setting reads as, so it must stay 0.</summary>
    Automatic = 0,

    /// <summary>One row per stack of identical items, on every save.</summary>
    Always,

    /// <summary>One row per item, on every save, up to <see cref="InventoryGroup.MaxItemsListedOneByOne"/> per card.</summary>
    Never,
}
