using System.Text;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class LogisticsGroupCatalogTests
{
    private static readonly LogisticsGroupCatalog Embedded = new();

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
        // Seeded list in gen_logistics_groups.py; a regression guard that curation kept up.
        string[] expected = ["Iron", "Cobalt", "Magnesium", "Vegetable0Growable", "Fish8Eggs", "Rod-osmium", "ToxicWater"];

        Assert.All(expected, id => Assert.True(Embedded.Resolve(id).IsKnown, id));
    }

    [Fact]
    public void Constructor_EmptyStream_Throws()
    {
        Assert.ThrowsAny<Exception>(() => new LogisticsGroupCatalog(new MemoryStream(Encoding.UTF8.GetBytes(""))));
    }
}
