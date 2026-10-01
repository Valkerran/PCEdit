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
