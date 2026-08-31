namespace PCEdit.App.Core.Models;

public sealed class InventoryGroup
{
    public required int InventoryId { get; init; }

    public required string Label { get; init; }

    public required int Size { get; init; }

    public required InventoryKind Kind { get; init; }

    /// <summary>
    /// The planet (world) this inventory belongs to: a player/equipment inventory's owner's
    /// <c>PlanetId</c>, or a container's resolved <c>WorldObject.Planet</c>. Null when the
    /// inventory has no owner (an orphan inventory) or the world could not be resolved.
    /// </summary>
    public string? PlanetId { get; init; }

    public required List<InventoryItemView> Items { get; init; }

    /// <summary>
    /// Logistics config, when this inventory is a logistics container (the game writes a
    /// <c>priority</c> key on those and only those). Null on a plain inventory.
    /// </summary>
    public LogisticsConfig? Logistics { get; init; }

    /// <summary>Pre-formatted "Demand N · Supply N · Priority X" line for the card; null on a plain inventory.</summary>
    public string? LogisticsSummary { get; init; }

    public bool IsLogisticsContainer => Logistics is not null;

    public int Count => Items.Count;

    public bool HasItems => Count > 0;

    public string CapacityLabel => $"{Count}/{Size}";

    /// <summary>Lower-cased haystack for the Inventories page search: the label, the world id,
    /// plus every contained item's display name.</summary>
    public string SearchIndex => _searchIndex ??=
        string.Join('\n', Items.Select(i => i.DisplayName).Prepend(PlanetId ?? string.Empty).Prepend(Label)).ToLowerInvariant();

    private string? _searchIndex;

    /// <summary>True when this group matches a (already lower-cased, trimmed) search term.</summary>
    public bool Matches(string loweredQuery) =>
        loweredQuery.Length == 0 || SearchIndex.Contains(loweredQuery, StringComparison.Ordinal);
}
