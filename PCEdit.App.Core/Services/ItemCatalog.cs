using System.Text.Json;
using System.Text.Json.Serialization;
using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// <see cref="IItemCatalog"/> backed by the embedded <c>Data/ItemCatalog.json</c>
/// dataset. The file is read once at construction (it is a few KB) and held in
/// memory. Regenerate the JSON with <c>tools/item-catalog/gen_catalog.py</c>.
/// </summary>
public sealed class ItemCatalog : IItemCatalog
{
    private const string ResourceSuffix = "Data.ItemCatalog.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, ItemEntry> _items;
    private readonly IReadOnlyDictionary<string, CategoryEntry> _categories;
    private readonly string _fallbackIcon;

    /// <summary>Loads the catalog from the embedded dataset.</summary>
    public ItemCatalog()
        : this(OpenEmbeddedResource())
    {
    }

    /// <summary>Loads the catalog from an arbitrary JSON stream (used by tests).</summary>
    internal ItemCatalog(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var document = JsonSerializer.Deserialize<CatalogDocument>(json, SerializerOptions)
            ?? throw new InvalidDataException("The item catalog stream is empty or invalid.");

        _categories = document.Categories;
        _items = document.Items;
        _fallbackIcon = document.Categories.TryGetValue(document.FallbackCategory, out var fallback)
            ? fallback.Icon
            : "cat_misc.png";
        All = document.Items.Select(pair => CreateInfo(pair.Key, pair.Value)).ToList();
    }

    public IReadOnlyList<ItemCatalogInfo> All { get; }

    public ItemDisplayInfo Resolve(string gId)
    {
        var info = ResolveInfo(gId);
        return new ItemDisplayInfo(info.GId, info.DisplayName, info.IconFile);
    }

    public ItemCatalogInfo ResolveInfo(string gId)
    {
        ArgumentNullException.ThrowIfNull(gId);

        return _items.TryGetValue(gId, out var item)
            ? CreateInfo(gId, item)
            : new ItemCatalogInfo(
                gId,
                gId,
                _fallbackIcon,
                CanDemand: false,
                CanSupply: false,
                IsDeprecated: false,
                AddedIn: null,
                DeprecatedIn: null,
                IsKnown: false);
    }

    private ItemCatalogInfo CreateInfo(string gId, ItemEntry item)
    {
        var icon = item.Icon;
        if (string.IsNullOrEmpty(icon) && _categories.TryGetValue(item.Category, out var category))
        {
            icon = category.Icon;
        }

        return new ItemCatalogInfo(
            gId,
            string.IsNullOrEmpty(item.DisplayName) ? gId : item.DisplayName,
            string.IsNullOrEmpty(icon) ? _fallbackIcon : icon,
            item.CanDemand,
            item.CanSupply,
            item.Deprecated,
            item.AddedIn,
            item.DeprecatedIn,
            IsKnown: true);
    }

    private static Stream OpenEmbeddedResource()
    {
        var assembly = typeof(ItemCatalog).Assembly;
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Embedded item catalog resource ('*{ResourceSuffix}') was not found.");

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded item catalog resource '{resourceName}' could not be opened.");
    }

    private sealed class CatalogDocument
    {
        [JsonPropertyName("fallbackCategory")]
        public string FallbackCategory { get; init; } = "misc";

        [JsonPropertyName("categories")]
        public Dictionary<string, CategoryEntry> Categories { get; init; } = new();

        [JsonPropertyName("items")]
        public Dictionary<string, ItemEntry> Items { get; init; } = new();
    }

    private sealed class CategoryEntry
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; init; } = string.Empty;
    }

    private sealed class ItemEntry
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("icon")]
        public string? Icon { get; init; }

        [JsonPropertyName("canDemand")]
        public bool CanDemand { get; init; }

        [JsonPropertyName("canSupply")]
        public bool CanSupply { get; init; }

        [JsonPropertyName("deprecated")]
        public bool Deprecated { get; init; }

        [JsonPropertyName("addedIn")]
        public string? AddedIn { get; init; }

        [JsonPropertyName("deprecatedIn")]
        public string? DeprecatedIn { get; init; }
    }
}
