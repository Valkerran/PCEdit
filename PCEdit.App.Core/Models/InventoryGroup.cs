namespace PCEdit.App.Core.Models;

public sealed class InventoryGroup
{
    public required int InventoryId { get; init; }

    public required string Label { get; init; }

    public required int Size { get; init; }

    public required InventoryKind Kind { get; init; }

    public required List<InventoryItemView> Items { get; init; }

    public int Count => Items.Count;

    public bool HasItems => Count > 0;

    public string CapacityLabel => $"{Count}/{Size}";

    /// <summary>Lower-cased haystack for the Inventories page search: the label plus every
    /// contained item's display name.</summary>
    public string SearchIndex => _searchIndex ??=
        string.Join('\n', Items.Select(i => i.DisplayName).Prepend(Label)).ToLowerInvariant();

    private string? _searchIndex;

    /// <summary>True when this group matches a (already lower-cased, trimmed) search term.</summary>
    public bool Matches(string loweredQuery) =>
        loweredQuery.Length == 0 || SearchIndex.Contains(loweredQuery, StringComparison.Ordinal);
}
