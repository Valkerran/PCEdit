using System.Text.Json;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.Services;

public sealed class ItemStackKeyTests
{
    private static IReadOnlyDictionary<string, JsonElement> Extra(params (string Key, string Json)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => JsonDocument.Parse(p.Json).RootElement.Clone());

    [Fact]
    public void ItemsDifferingOnlyByTheirId_ShareAKey()
    {
        Assert.Equal(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "Iron" }),
            ItemStackKey.Of(new WorldObject { Id = 2, GId = "Iron" }));
    }

    [Fact]
    public void ItemsOfDifferentTypes_HaveDifferentKeys()
    {
        Assert.NotEqual(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "Iron" }),
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "Cobalt" }));
    }

    [Fact]
    public void GeneticTraitsWithDifferentTraits_HaveDifferentKeys()
    {
        // Real shape from Interplanetary-2.102.json: each trait carries its colour and trtInd.
        var color = "0.9137255-0.5921569-0.4862745-0";
        Assert.NotEqual(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "GeneticTrait", Color = color, ExtensionData = Extra(("trtInd", "3")) }),
            ItemStackKey.Of(new WorldObject { Id = 2, GId = "GeneticTrait", Color = color, ExtensionData = Extra(("trtInd", "4")) }));
    }

    [Fact]
    public void ItemsDifferingInANamedField_HaveDifferentKeys()
    {
        Assert.NotEqual(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "GeneticTrait", Color = "1-0-0-0" }),
            ItemStackKey.Of(new WorldObject { Id = 2, GId = "GeneticTrait", Color = "0-1-0-0" }));
    }

    [Fact]
    public void ItemsDifferingOnlyInWhereTheyLay_ShareAKey()
    {
        // Real shape from a stacking-mod save: every Eggplant in a Food Grower keeps the random
        // rotation it was spawned with. Position, rotation and planet say where an object sat in
        // the world, not what the item is - 100 of them must be one row, not 100.
        Assert.Equal(
            ItemStackKey.Of(new WorldObject
            {
                Id = 205710267, GId = "Vegetable0Growable", Growth = 100, Planet = -1140328421,
                Position = "381.2548,145.2848,970.28", Rotation = "0.05755999,0.3741006,-0.026139,-0.925231",
            }),
            ItemStackKey.Of(new WorldObject
            {
                Id = 209207763, GId = "Vegetable0Growable", Growth = 100, Planet = 1,
                Position = "0,0,0", Rotation = "0.03698213,0.7841589,-0.05127106,-0.6173317",
            }));
    }

    [Fact]
    public void AFieldPresentOnOnlyOneItem_SeparatesThem()
    {
        Assert.NotEqual(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "Seed" }),
            ItemStackKey.Of(new WorldObject { Id = 2, GId = "Seed", Growth = 50 }));
    }

    [Fact]
    public void TheOrderOfUnknownKeys_DoesNotMatter()
    {
        // The game's key order is not stable between records (see WorldObjectConverter).
        Assert.Equal(
            ItemStackKey.Of(new WorldObject { Id = 1, GId = "X", ExtensionData = Extra(("a", "1"), ("b", "\"x\"")) }),
            ItemStackKey.Of(new WorldObject { Id = 2, GId = "X", ExtensionData = Extra(("b", "\"x\""), ("a", "1")) }));
    }
}
