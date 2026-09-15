using System.Text;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class LogisticsGroupCatalogTests
{
    private static readonly LogisticsGroupCatalog Embedded = new(new ItemCatalog());

    [Fact]
    public void All_IsNonEmpty_AndOrderedByDisplayName()
    {
        var names = Embedded.All.Select(g => g.DisplayName).ToList();

        Assert.NotEmpty(names);
        Assert.Equal(names.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase), names);
        Assert.All(Embedded.All, g => Assert.False(string.IsNullOrWhiteSpace(g.DisplayName)));
    }

    [Fact]
    public void Resolve_KnownGroup_ReturnsFriendlyName()
    {
        var info = Embedded.Resolve("Uranim");

        Assert.Equal("Uranim", info.Id);
        Assert.Equal("Uranium", info.DisplayName);
        Assert.True(info.IsKnown);
    }

    [Fact]
    public void Resolve_KnownGroupWhoseNameEqualsItsId_IsStillKnown()
    {
        var info = Embedded.Resolve("Iron");

        Assert.Equal("Iron", info.DisplayName);
        Assert.True(info.IsKnown);
    }

    [Fact]
    public void Resolve_UnknownGroup_MapsToItselfAndIsFlaggedUnknown()
    {
        var info = Embedded.Resolve("SomeGroupANewGameBuildAdded");

        Assert.Equal("SomeGroupANewGameBuildAdded", info.Id);
        Assert.Equal("SomeGroupANewGameBuildAdded", info.DisplayName);
        Assert.False(info.IsKnown);
    }

    [Fact]
    public void Catalog_CoversEveryGroupIdUsedByTheSampleSaves()
    {
        // Seeded in gen_catalog.py; a regression guard that curation kept up.
        string[] expected = ["Iron", "Cobalt", "Magnesium", "Vegetable0Growable", "Fish8Eggs", "Rod-osmium", "ToxicWater"];

        Assert.All(expected, id =>
        {
            Assert.True(Embedded.Resolve(id).IsKnown, id);
            Assert.Contains(Embedded.DemandGroups, group => group.Id == id);
            Assert.Contains(Embedded.SupplyGroups, group => group.Id == id);
        });
    }

    [Fact]
    public void DirectionalLists_RespectCapabilities_AndExcludeDeprecatedItems()
    {
        var itemCatalog = new ItemCatalog(new MemoryStream(Encoding.UTF8.GetBytes("""
        {
          "fallbackCategory": "misc",
          "categories": {
            "misc": { "displayName": "Miscellaneous", "icon": "cat_misc.png" }
          },
          "items": {
            "DemandOnly": {
              "displayName": "Demand Only", "category": "misc",
              "canDemand": true, "canSupply": false, "deprecated": false,
              "addedIn": "1.0", "deprecatedIn": null
            },
            "SupplyOnly": {
              "displayName": "Supply Only", "category": "misc",
              "canDemand": false, "canSupply": true, "deprecated": false,
              "addedIn": null, "deprecatedIn": null
            },
            "OldItem": {
              "displayName": "Old Item", "category": "misc",
              "canDemand": true, "canSupply": true, "deprecated": true,
              "addedIn": "0.1", "deprecatedIn": "2.0"
            }
          }
        }
        """)));
        var catalog = new LogisticsGroupCatalog(itemCatalog);

        Assert.Equal(["DemandOnly"], catalog.DemandGroups.Select(group => group.Id));
        Assert.Equal(["SupplyOnly"], catalog.SupplyGroups.Select(group => group.Id));
        Assert.True(catalog.Resolve("OldItem").IsKnown);
        Assert.DoesNotContain(catalog.All, group => group.Id == "OldItem");
    }
}
