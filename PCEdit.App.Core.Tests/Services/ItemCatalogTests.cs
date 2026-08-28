using System.Text;
using System.Text.Json;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class ItemCatalogTests
{
    private static readonly ItemCatalog EmbeddedCatalog = new();

    [Fact]
    public void Resolve_KnownItem_ReturnsFriendlyNameAndCategoryIcon()
    {
        var info = EmbeddedCatalog.Resolve("Uranim");

        Assert.Equal("Uranim", info.GId);
        Assert.Equal("Uranium", info.DisplayName);
        Assert.Equal("cat_ore.png", info.IconFile);
    }

    [Fact]
    public void Resolve_UnknownItem_FallsBackToRawIdAndFallbackIcon()
    {
        var info = EmbeddedCatalog.Resolve("TotallyMadeUpGId");

        Assert.Equal("TotallyMadeUpGId", info.GId);
        Assert.Equal("TotallyMadeUpGId", info.DisplayName);
        Assert.Equal("cat_misc.png", info.IconFile);
    }

    [Fact]
    public void Resolve_ItemWithoutIconOverride_UsesCategoryIcon()
    {
        var catalog = CatalogFrom("""
        {
          "fallbackCategory": "misc",
          "categories": {
            "misc": { "displayName": "Miscellaneous", "icon": "cat_misc.png" },
            "ore":  { "displayName": "Ore", "icon": "cat_ore.png" }
          },
          "items": {
            "Rock": { "displayName": "Rock", "category": "ore" }
          }
        }
        """);

        Assert.Equal("cat_ore.png", catalog.Resolve("Rock").IconFile);
    }

    [Fact]
    public void Resolve_ItemIconOverride_WinsOverCategoryIcon()
    {
        var catalog = CatalogFrom("""
        {
          "fallbackCategory": "misc",
          "categories": {
            "misc": { "displayName": "Miscellaneous", "icon": "cat_misc.png" },
            "ore":  { "displayName": "Ore", "icon": "cat_ore.png" }
          },
          "items": {
            "Shiny": { "displayName": "Shiny Thing", "category": "ore", "icon": "item_shiny.png" }
          }
        }
        """);

        Assert.Equal("item_shiny.png", catalog.Resolve("Shiny").IconFile);
    }

    [Fact]
    public void EmbeddedCatalog_ParsesAndEveryItemCategoryIsDefined()
    {
        using var stream = OpenEmbeddedCatalog();
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var categoryNames = root.GetProperty("categories")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(categoryNames);
        Assert.Contains(root.GetProperty("fallbackCategory").GetString() ?? "", categoryNames);

        foreach (var item in root.GetProperty("items").EnumerateObject())
        {
            var category = item.Value.GetProperty("category").GetString() ?? "";
            Assert.True(
                categoryNames.Contains(category),
                $"Item '{item.Name}' references undefined category '{category}'.");
            Assert.False(
                string.IsNullOrWhiteSpace(item.Value.GetProperty("displayName").GetString()),
                $"Item '{item.Name}' has a blank displayName.");
        }
    }

    private static ItemCatalog CatalogFrom(string json)
        => new(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    private static Stream OpenEmbeddedCatalog()
    {
        var assembly = typeof(ItemCatalog).Assembly;
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            n => n.EndsWith("Data.ItemCatalog.json", StringComparison.Ordinal))!;
        return assembly.GetManifestResourceStream(resourceName)!;
    }
}
