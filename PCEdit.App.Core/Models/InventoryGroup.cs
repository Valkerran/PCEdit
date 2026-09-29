using PCEdit.App.Core.Services;

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

    /// <summary>
    /// The world-object id of the container that owns this inventory, when it has one. It is
    /// not the inventory id - the two match for only a minority of containers - but it is the
    /// "Object #" the card's label shows, so an id search matches it too.
    /// </summary>
    public int? ContainerWorldObjectId { get; init; }

    public required List<InventoryItemView> Items { get; init; }

    /// <summary>
    /// Logistics config, when this inventory is a logistics container (the game writes a
    /// <c>priority</c> key on those and only those). Null on a plain inventory.
    /// </summary>
    public LogisticsConfig? Logistics { get; init; }

    /// <summary>Pre-formatted "Demand N · Supply N · Priority X" line for the card; null on a plain inventory.</summary>
    public string? LogisticsSummary { get; init; }

    public bool IsLogisticsContainer => Logistics is not null;

    /// <summary>Whether the card shows the inventory id under its label. An unowned inventory's
    /// label already is "Inventory #N", so repeating it would say the same thing twice.</summary>
    public bool ShowInventoryIdCaption => Kind != InventoryKind.Other;

    public int Count => Items.Count;

    public bool HasItems => Count > 0;

    public string CapacityLabel => $"{Count}/{Size}";

    /// <summary>Lower-cased haystack for the Inventories page text search: the label, the world
    /// id, plus every contained item's display name and type id (<c>GId</c>, e.g. <c>Iron</c>).</summary>
    public string SearchIndex => _searchIndex ??=
        string.Join('\n', Items.SelectMany(i => new[] { i.DisplayName, i.GId })
            .Prepend(PlanetId ?? string.Empty).Prepend(Label)).ToLowerInvariant();

    private string? _searchIndex;

    /// <summary>
    /// True when this group matches a (already lower-cased, trimmed) search term. An id search
    /// (see <see cref="IdSearch"/>) matches the inventory id, the owning container's object id
    /// and every contained item's id by prefix; anything else is a text search over
    /// <see cref="SearchIndex"/>.
    /// </summary>
    public bool Matches(string loweredQuery)
    {
        if (loweredQuery.Length == 0)
        {
            return true;
        }

        if (IdSearch.TryGetIdPrefix(loweredQuery, out var digits))
        {
            return IdSearch.Matches(InventoryId, digits)
                   || (ContainerWorldObjectId is { } containerId && IdSearch.Matches(containerId, digits))
                   || Items.Any(i => IdSearch.Matches(i.WorldObjectId, digits));
        }

        return SearchIndex.Contains(loweredQuery, StringComparison.Ordinal);
    }
}
