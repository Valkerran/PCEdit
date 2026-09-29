using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Tests.Models;

public sealed class InventoryGroupTests
{
    // A container whose label embeds its object id (as real labels do: "Storage (Object #1880)"),
    // with an inventory id that differs from it, holding one item whose display name and type id
    // differ - the case the fixture save cannot express, since its GIds are not in the catalog.
    private static InventoryGroup Container(InventoryKind kind = InventoryKind.Container) => new()
    {
        InventoryId = 5,
        Label = "Storage (Object #1880)",
        Size = 10,
        Kind = kind,
        ContainerWorldObjectId = 1880,
        Items = [new InventoryItemView(4021, "Tree12Seed", 5, "Brojo Seed", "seed.png")],
    };

    [Theory]
    [InlineData("5")]      // inventory id
    [InlineData("#5")]
    [InlineData("188")]    // container object id, by prefix
    [InlineData("402")]    // item id, by prefix
    [InlineData("#4021")]
    public void IdSearch_MatchesInventoryContainerAndItemIdsByPrefix(string query)
    {
        Assert.True(Container().Matches(query));
    }

    [Fact]
    public void IdSearch_DoesNotFallBackToTheLabelText()
    {
        // "88" appears inside the label's "#1880", but is not a prefix of any id.
        Assert.False(Container().Matches("88"));
    }

    [Theory]
    [InlineData("tree12seed")] // item type id (GId)
    [InlineData("brojo")]      // item display name
    [InlineData("storage")]    // label
    public void TextSearch_MatchesLabelItemNamesAndTypeIds(string loweredQuery)
    {
        Assert.True(Container().Matches(loweredQuery));
    }

    [Theory]
    [InlineData(InventoryKind.PlayerInventory, true)]
    [InlineData(InventoryKind.Equipment, true)]
    [InlineData(InventoryKind.Container, true)]
    [InlineData(InventoryKind.Other, false)] // its label already reads "Inventory #N"
    public void ShowInventoryIdCaption_IsHiddenOnlyWhereTheLabelAlreadyIsTheId(InventoryKind kind, bool expected)
    {
        Assert.Equal(expected, Container(kind).ShowInventoryIdCaption);
    }

    [Fact]
    public void EmptyQuery_MatchesEverything()
    {
        Assert.True(Container().Matches(string.Empty));
    }
}
