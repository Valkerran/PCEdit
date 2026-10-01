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

    [Fact]
    public void Stacks_GroupItemsOfOneKind_InTheOrderTheyFirstAppear()
    {
        var stacks = MixedContainer().Stacks;

        Assert.Equal(["Brojo Seed", "Iron"], stacks.Select(s => s.DisplayName).ToArray());
        Assert.Equal([4021], stacks[0].WorldObjectIds);
        Assert.Equal([4022, 5100], stacks[1].WorldObjectIds);
    }

    [Fact]
    public void Stacks_KeepItemsWithDifferentStackKeysApart()
    {
        // Two traits of the same type but different trait data must stay separately movable.
        var card = MixedContainer() with
        {
            Items =
            [
                new InventoryItemView(1, "GeneticTrait", 5, "Genetic Trait", "dna.png") { StackKey = "trait-a" },
                new InventoryItemView(2, "GeneticTrait", 5, "Genetic Trait", "dna.png") { StackKey = "trait-b" },
                new InventoryItemView(3, "GeneticTrait", 5, "Genetic Trait", "dna.png") { StackKey = "trait-a" },
            ],
        };

        Assert.Equal([[1, 3], [2]], card.Stacks.Select(s => s.WorldObjectIds.ToArray()).ToArray());
    }

    [Fact]
    public void AStack_ReportsItsCountAndTheItemAMoveTakes()
    {
        var iron = MixedContainer().Stacks[1];

        Assert.Equal(2, iron.Count);
        Assert.False(iron.IsSingle);
        Assert.Equal(5100, iron.LastWorldObjectId); // the last in the save's order, like the game's overflow
    }

    [Fact]
    public void NarrowTo_ATextSearch_StillGroupsTheMatches()
    {
        var narrowed = MixedContainer().NarrowTo("iron")!;

        var stack = Assert.Single(narrowed.Stacks);
        Assert.Equal([4022, 5100], stack.WorldObjectIds);
    }

    [Fact]
    public void NarrowTo_AnIdSearch_ListsEachMatchingItemOnItsOwnRow()
    {
        // Searching by id is how a player finds one particular item; grouping it back into a
        // stack would hide which one a Move would take.
        var card = MixedContainer() with
        {
            Items =
            [
                new InventoryItemView(4022, "Iron", 5, "Iron", "ore.png"),
                new InventoryItemView(4023, "Iron", 5, "Iron", "ore.png"),
                new InventoryItemView(5100, "Iron", 5, "Iron", "ore.png"),
            ],
        };

        var narrowed = card.NarrowTo("402")!;

        Assert.Equal([[4022], [4023]], narrowed.Stacks.Select(s => s.WorldObjectIds.ToArray()).ToArray());
        Assert.All(narrowed.Stacks, s => Assert.True(s.IsSingle));
    }

    // `count` Iron items in one card - the same stack, so grouped they make one row.
    // No container object id, so an id search can only match the items.
    private static InventoryGroup Irons(int count) => MixedContainer() with
    {
        ContainerWorldObjectId = null,
        Items = Enumerable.Range(1, count).Select(id => new InventoryItemView(id, "Iron", 5, "Iron", "ore.png")).ToList(),
    };

    [Fact]
    public void ListEachItem_GivesEveryItemItsOwnRow()
    {
        var card = Irons(3) with { ListEachItem = true };

        Assert.Equal(3, card.Stacks.Count);
        Assert.All(card.Stacks, s => Assert.True(s.IsSingle));
        Assert.False(card.IsGroupedForSize);
    }

    [Fact]
    public void ListEachItem_OnACardTooBigToList_StillGroups_AndSaysSo()
    {
        // A row per item on a stacking-mod chest froze the page for seconds (issue #64).
        var card = Irons(InventoryGroup.MaxItemsListedOneByOne + 1) with { ListEachItem = true };

        Assert.Single(card.Stacks);
        Assert.True(card.IsGroupedForSize);
    }

    [Fact]
    public void AnIdSearchMatchingTooManyItems_StillGroups()
    {
        // Ids 1, 10-19, 100-199 and 1000-1999 start with "1": 1,111 matches, past the limit.
        var narrowed = Irons(2000).NarrowTo("1")!;

        Assert.Equal(1111, narrowed.Items.Count);
        Assert.True(narrowed.IsGroupedForSize);
        Assert.Single(narrowed.Stacks);
    }

    [Fact]
    public void AGroupedCard_IsNotFlaggedAsGroupedForSize()
    {
        Assert.False(Irons(InventoryGroup.MaxItemsListedOneByOne + 1).IsGroupedForSize);
    }

    // A card holding `count` items in `size` slots; the items themselves don't matter to the fill.
    private static InventoryGroup Filled(int count, int size) => new()
    {
        InventoryId = 7,
        Label = "Storage",
        Size = size,
        Kind = InventoryKind.Container,
        Items = [],
        TotalItemCount = count,
    };

    [Theory]
    [InlineData(0, 15, InventoryFill.Normal)]
    [InlineData(15, 15, InventoryFill.Normal)]            // full is not over-full
    [InlineData(16, 15, InventoryFill.OverFull)]
    [InlineData(1500, 15, InventoryFill.OverFull)]        // a 100x stacking-mod chest
    [InlineData(7200, 80, InventoryFill.OverFull)]
    [InlineData(7201, 80, InventoryFill.NearLoadLimit)]   // past 90% of the game's 8000-item load cap
    [InlineData(7201, 10000, InventoryFill.NearLoadLimit)] // the cap applies whatever the size
    public void Fill_ClassifiesTheItemCountAgainstTheSizeAndTheGamesLoadCap(int count, int size, InventoryFill expected)
    {
        Assert.Equal(expected, Filled(count, size).Fill);
    }

    [Fact]
    public void Fill_OfANarrowedCard_UsesTheTrueItemCount()
    {
        var overFull = MixedContainer() with { Size = 2 };

        var narrowed = overFull.NarrowTo("brojo")!;

        Assert.Single(narrowed.Items);
        Assert.Equal(InventoryFill.OverFull, narrowed.Fill);
    }

    [Theory]
    [InlineData(15, 15, false, false)]
    [InlineData(16, 15, true, false)]
    [InlineData(7201, 80, false, true)] // near the load limit replaces over-full, never both
    public void IsOverFullAndIsNearLoadLimit_SelectExactlyOneBadgeState(int count, int size, bool overFull, bool nearLimit)
    {
        var card = Filled(count, size);

        Assert.Equal(overFull, card.IsOverFull);
        Assert.Equal(nearLimit, card.IsNearLoadLimit);
    }

    [Theory]
    [InlineData(15, 15, false)]
    [InlineData(16, 15, true)]
    [InlineData(7201, 10000, true)]
    public void NeedsAttention_IsTrueForAnyFillBeyondNormal(int count, int size, bool expected)
    {
        Assert.Equal(expected, Filled(count, size).NeedsAttention);
    }
}
