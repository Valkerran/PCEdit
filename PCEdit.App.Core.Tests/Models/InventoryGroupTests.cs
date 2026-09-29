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

    // Three items, two of them Iron, so a search can narrow the card to part of its contents.
    private static InventoryGroup MixedContainer() => new()
    {
        InventoryId = 5,
        Label = "Storage (Object #1880)",
        Size = 10,
        Kind = InventoryKind.Container,
        ContainerWorldObjectId = 1880,
        PlanetId = "Prime",
        Items =
        [
            new InventoryItemView(4021, "Tree12Seed", 5, "Brojo Seed", "seed.png"),
            new InventoryItemView(4022, "Iron", 5, "Iron", "ore.png"),
            new InventoryItemView(5100, "Iron", 5, "Iron", "ore.png"),
        ],
    };

    [Fact]
    public void NarrowTo_AnItemOnlyMatch_ListsJustTheMatchingItems_ButKeepsTheTrueFill()
    {
        var narrowed = MixedContainer().NarrowTo("iron");

        Assert.NotNull(narrowed);
        Assert.Equal([4022, 5100], narrowed.Items.Select(i => i.WorldObjectId).ToArray());
        Assert.True(narrowed.IsNarrowed);
        Assert.Equal(3, narrowed.TotalItemCount);
        Assert.Equal("3/10", narrowed.CapacityLabel); // the real fill, not the match count
        Assert.True(narrowed.HasItems);
    }

    [Fact]
    public void NarrowTo_AnItemIdPrefix_ListsJustThoseItems()
    {
        var narrowed = MixedContainer().NarrowTo("402");

        Assert.Equal([4021, 4022], narrowed!.Items.Select(i => i.WorldObjectId).ToArray());
    }

    [Theory]
    [InlineData("storage")] // label
    [InlineData("prime")]   // world
    [InlineData("5")]       // inventory id - even though item 5100 also starts with 5
    [InlineData("#188")]    // container object id
    public void NarrowTo_WhenTheInventoryItselfMatches_KeepsTheWholeCard(string loweredQuery)
    {
        var group = MixedContainer();

        var shown = group.NarrowTo(loweredQuery);

        Assert.Same(group, shown);
        Assert.False(shown!.IsNarrowed);
    }

    [Fact]
    public void NarrowTo_WhenEveryItemMatches_KeepsTheWholeCard()
    {
        var group = MixedContainer();

        Assert.Same(group, group.NarrowTo("o")); // in "Brojo Seed" and both "Iron"s
    }

    [Fact]
    public void NarrowTo_WithNoMatch_HidesTheCard()
    {
        Assert.Null(MixedContainer().NarrowTo("zzz"));
    }

    [Fact]
    public void NarrowTo_LeavesTheOriginalCardUntouched()
    {
        var group = MixedContainer();

        _ = group.NarrowTo("iron");

        Assert.Equal(3, group.Items.Count);
        Assert.False(group.IsNarrowed);
    }

    [Fact]
    public void EmptyQuery_MatchesEverything()
    {
        Assert.True(Container().Matches(string.Empty));
    }
}
