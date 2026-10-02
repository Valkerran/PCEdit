using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Models;

public sealed record InventoryOptionView(
    int InventoryId,
    string Label,
    int Count,
    int Size,
    InventoryKind Kind = InventoryKind.Other,
    int? ContainerWorldObjectId = null)
{
    public bool IsFull => Count >= Size;

    /// <summary>How many items the move needs room for; set by the dialog from its quantity.</summary>
    public int RequiredRoom { get; init; } = 1;

    /// <summary>Free slots against the stored size - never negative, even for an over-full inventory.</summary>
    public int Free => Math.Max(0, Size - Count);

    /// <summary>Whether this destination can take the move; the dialog disables it otherwise.</summary>
    public bool HasRoom => Free >= RequiredRoom;

    public string CapacityLabel => $"{Count}/{Size}";

    /// <summary>Accessible name for the destination button, including why it is disabled. Full
    /// inventories are not offered at all, so the reason is always the room left.</summary>
    public string AccessibleLabel => HasRoom
        ? $"{Label}, {CapacityLabel}"
        : $"{Label}, room for {Free}, {CapacityLabel}";

    /// <summary>Whether the destination shows its inventory id under its label - as on the
    /// Inventories page, not for an unowned inventory whose label already is "Inventory #N".</summary>
    public bool ShowInventoryIdCaption => Kind != InventoryKind.Other;

    /// <summary>
    /// True when this destination matches a search term. An id search (see
    /// <see cref="IdSearch"/>) matches the inventory id and the owning container's object id by
    /// prefix - the same rule as the Inventories page; anything else matches the label.
    /// </summary>
    public bool Matches(string term)
    {
        var trimmed = term.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        if (IdSearch.TryGetIdPrefix(trimmed, out var digits))
        {
            return IdSearch.Matches(InventoryId, digits)
                   || (ContainerWorldObjectId is { } containerId && IdSearch.Matches(containerId, digits));
        }

        return Label.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
