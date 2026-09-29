using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Models;

/// <summary>
/// One card on the Inventories page. A record so that <see cref="NarrowTo"/> can make a filtered
/// copy with a <c>with</c> expression - the copy must carry every other property unchanged.
/// </summary>
public sealed record InventoryGroup
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

    /// <summary>The items the card lists: every item, or only the matching ones on a card
    /// narrowed by a search (see <see cref="NarrowTo"/>).</summary>
    public required List<InventoryItemView> Items { get; init; }

    /// <summary>
    /// How many items the inventory really holds. Differs from <c>Items.Count</c> only on a
    /// narrowed card, whose capacity badge must still show the true fill, not the match count.
    /// </summary>
    public int TotalItemCount
    {
        get => _totalItemCount ?? Items.Count;
        init => _totalItemCount = value;
    }

    private readonly int? _totalItemCount;

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

    public bool HasItems => TotalItemCount > 0;

    public string CapacityLabel => $"{TotalItemCount}/{Size}";

    /// <summary>True when a search narrowed the card to some of its items.</summary>
    public bool IsNarrowed => Items.Count < TotalItemCount;

    /// <summary>
    /// The card to show for a (already lower-cased, trimmed) search term, or null to hide it.
    /// When the inventory itself matches - its label, world, inventory id or container object
    /// id - the card is returned whole: the user searched for the inventory. Otherwise it is
    /// narrowed to just the matching items, and hidden if none match.
    /// </summary>
    public InventoryGroup? NarrowTo(string loweredQuery)
    {
        if (MatchesInventory(loweredQuery))
        {
            return this;
        }

        var digits = IdSearch.TryGetIdPrefix(loweredQuery, out var prefix) ? prefix : null;
        var matching = Items.Where(item => MatchesItem(item, loweredQuery, digits)).ToList();
        if (matching.Count == 0)
        {
            return null;
        }

        return matching.Count == Items.Count
            ? this
            : this with { Items = matching, TotalItemCount = TotalItemCount };
    }

    /// <summary>True when this card would be shown at all for a search term.</summary>
    public bool Matches(string loweredQuery) => NarrowTo(loweredQuery) is not null;

    /// <summary>
    /// The inventory's own fields. An id search (see <see cref="IdSearch"/>) matches the
    /// inventory id and the owning container's object id by prefix; any other term matches the
    /// label or the world. An empty term matches everything.
    /// </summary>
    private bool MatchesInventory(string loweredQuery)
    {
        if (loweredQuery.Length == 0)
        {
            return true;
        }

        if (IdSearch.TryGetIdPrefix(loweredQuery, out var digits))
        {
            return IdSearch.Matches(InventoryId, digits)
                   || (ContainerWorldObjectId is { } containerId && IdSearch.Matches(containerId, digits));
        }

        return Label.Contains(loweredQuery, StringComparison.OrdinalIgnoreCase)
               || (PlanetId?.Contains(loweredQuery, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>
    /// One item. An id search (<paramref name="idDigits"/> non-null, parsed once per card rather
    /// than per item) matches the item's world-object id by prefix; any other term matches its
    /// display name or its type id (<c>GId</c>, e.g. <c>Iron</c>).
    /// </summary>
    private static bool MatchesItem(InventoryItemView item, string loweredQuery, string? idDigits)
    {
        if (idDigits is not null)
        {
            return IdSearch.Matches(item.WorldObjectId, idDigits);
        }

        return item.DisplayName.Contains(loweredQuery, StringComparison.OrdinalIgnoreCase)
               || item.GId.Contains(loweredQuery, StringComparison.OrdinalIgnoreCase);
    }
}
