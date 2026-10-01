using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.Services;

public sealed class OverflowRepairTests
{
    private const string Path = @"C:\fake\save.txt";

    private static (OverflowRepair Repair, SaveFileWorkspace Workspace) CreateLoaded(Action<PlanetCrafterSaveFile>? adjust = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        adjust?.Invoke(save);
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        return (new OverflowRepair(workspace, new ItemCatalog()), workspace);
    }

    private static string IdsOf(SaveFileWorkspace workspace, int inventoryId) =>
        workspace.Current!.Inventories.Single(i => i.Id == inventoryId).WorldObjectIds;

    // Inventory 30 (the storage container, size 3) holding `count` Iron items, ids 1000 up.
    private static Action<PlanetCrafterSaveFile> FillContainer30(int count, int size = 3) => save =>
    {
        var ids = Enumerable.Range(1000, count).ToList();
        save.WorldObjects.AddRange(ids.Select(id => new WorldObject { Id = id, GId = "Iron" }));
        var index = save.Inventories.FindIndex(i => i.Id == 30);
        save.Inventories[index] = save.Inventories[index] with { WorldObjectIds = string.Join(',', ids), Size = size };
    };

    [Fact]
    public void Plan_OfAnUnmoddedSave_RemovesNothing()
    {
        var (repair, _) = CreateLoaded();

        var plan = repair.Plan(multiple: 1);

        Assert.Equal(0, plan.ItemCount);
        Assert.Equal(0, plan.ContainerCount);
        Assert.Empty(plan.ByType);
    }

    [Fact]
    public void Plan_ChangesNothing()
    {
        var (repair, workspace) = CreateLoaded(WorkspaceFixtures.OverFillBobsInventory);

        var plan = repair.Plan(multiple: 1);

        Assert.Equal(1, plan.ItemCount);
        Assert.Equal("200,201", IdsOf(workspace, 20));
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void Apply_AtOneTimesTheSize_RemovesTheLastItemsAndTheirRecords()
    {
        var (repair, workspace) = CreateLoaded(WorkspaceFixtures.OverFillBobsInventory); // 2 items, 1 slot

        var done = repair.Apply(multiple: 1);

        Assert.Equal(1, done.ItemCount);
        Assert.Equal(1, done.ContainerCount);
        Assert.Equal("200", IdsOf(workspace, 20));
        Assert.DoesNotContain(workspace.Current!.WorldObjects, w => w.Id == 201);
        Assert.Contains(workspace.Current!.WorldObjects, w => w.Id == 200);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void Apply_AtALargerMultiple_KeepsUpToThatManyTimesTheSize()
    {
        var (repair, workspace) = CreateLoaded(FillContainer30(10)); // size 3

        var done = repair.Apply(multiple: 2);

        Assert.Equal(4, done.ItemCount);
        Assert.Equal("1000,1001,1002,1003,1004,1005", IdsOf(workspace, 30));
    }

    [Fact]
    public void Apply_NeverKeepsMoreThanTheNearLoadLimit()
    {
        // A container the game would truncate on load is not repaired by keeping it that full.
        var (repair, workspace) = CreateLoaded(FillContainer30(7_300, size: 10_000));

        repair.Apply(multiple: 1);

        Assert.Equal(7_200, WorldObjectIdsCodec.Parse(IdsOf(workspace, 30)).Count);
    }

    [Fact]
    public void Apply_KeepsAnItemThatOwnsAnInventory()
    {
        // Deleting it would orphan the inventory it carries.
        var (repair, workspace) = CreateLoaded(save =>
        {
            WorkspaceFixtures.OverFillBobsInventory(save);
            var index = save.WorldObjects.FindIndex(w => w.Id == 201);
            save.WorldObjects[index] = save.WorldObjects[index] with { LinkedInventoryId = 99 };
        });

        repair.Apply(multiple: 1);

        Assert.Equal("201", IdsOf(workspace, 20)); // 200 went instead
        Assert.Contains(workspace.Current!.WorldObjects, w => w.Id == 201);
    }

    [Fact]
    public void Apply_KeepsTheRecordOfAnItemAnotherInventoryStillLists()
    {
        var (repair, workspace) = CreateLoaded(save =>
        {
            WorkspaceFixtures.OverFillBobsInventory(save);
            var alice = save.Inventories.FindIndex(i => i.Id == 10);
            save.Inventories[alice] = save.Inventories[alice] with { WorldObjectIds = "201" };
        });

        repair.Apply(multiple: 1);

        Assert.Equal("200", IdsOf(workspace, 20));
        Assert.Contains(workspace.Current!.WorldObjects, w => w.Id == 201);
    }

    [Fact]
    public void Apply_CarriesUnreadableEntriesThrough()
    {
        // Same rule as a move (issue #37): what PCEdit cannot read, it does not delete.
        var (repair, workspace) = CreateLoaded(save =>
        {
            WorkspaceFixtures.OverFillBobsInventory(save);
            var bob = save.Inventories.FindIndex(i => i.Id == 20);
            save.Inventories[bob] = save.Inventories[bob] with { WorldObjectIds = "200,junk,201" };
        });

        repair.Apply(multiple: 1);

        Assert.Equal("200,junk", IdsOf(workspace, 20));
    }

    [Fact]
    public void Plan_SummarisesTheRemovedItemsByType_MostFirst()
    {
        var (repair, _) = CreateLoaded(save =>
        {
            FillContainer30(5)(save); // 2 Iron over
            save.WorldObjects.Add(new WorldObject { Id = 2000, GId = "Cobalt" });
            save.WorldObjects.Add(new WorldObject { Id = 2001, GId = "Cobalt" });
            save.WorldObjects.Add(new WorldObject { Id = 2002, GId = "Cobalt" });
            var bob = save.Inventories.FindIndex(i => i.Id == 20);
            save.Inventories[bob] = save.Inventories[bob] with { WorldObjectIds = "2000,2001,2002" }; // 2 Cobalt over
            var alice = save.Inventories.FindIndex(i => i.Id == 11);
            save.Inventories[alice] = save.Inventories[alice] with { WorldObjectIds = "" };
        });

        var plan = repair.Plan(multiple: 1);

        Assert.Equal(4, plan.ItemCount);
        Assert.Equal(2, plan.ContainerCount);
        Assert.Equal([("Cobalt", 2), ("Iron", 2)], plan.ByType.Select(t => (t.DisplayName, t.Count)).ToArray());
    }

    // A placed Storage Crate (Container1) - world object `crateId`, inventory `inventoryId` - with
    // `size` slots holding `contents`, on `planet`; `logistics` gives it a priority as the game does.
    private static Action<PlanetCrafterSaveFile> Crate(int crateId, int inventoryId, int size,
        string contents = "", int? planet = null, bool logistics = false) => save =>
    {
        save.WorldObjects.Add(new WorldObject { Id = crateId, GId = "Container1", LinkedInventoryId = inventoryId, Planet = planet });
        save.Inventories.Add(new Inventory { Id = inventoryId, WorldObjectIds = contents, Size = size, Priority = logistics ? 0 : null });
    };

    private static Action<PlanetCrafterSaveFile> All(params Action<PlanetCrafterSaveFile>[] steps) => save =>
    {
        foreach (var step in steps)
        {
            step(save);
        }
    };

    private static void Cobalt(PlanetCrafterSaveFile save, params int[] ids) =>
        save.WorldObjects.AddRange(ids.Select(id => new WorldObject { Id = id, GId = "Cobalt" }));

    [Fact]
    public void Plan_ListsEveryExcessType_ForThePlayerToChooseFrom()
    {
        var (repair, _) = CreateLoaded(FillContainer30(6)); // 3 Iron over

        var plan = repair.Plan(multiple: 1);

        var iron = Assert.Single(plan.ExcessByType);
        Assert.Equal(("Iron", 3), (iron.GId, iron.Count));
        Assert.Equal(0, plan.MovedCount);
    }

    [Fact]
    public void Apply_MovesTickedTypesIntoFreeStorage_AndRemovesWhatDoesNotFit()
    {
        // 3 Iron over (1003, 1004, 1005), one crate with 2 free slots: the first two move, the last goes.
        var (repair, workspace) = CreateLoaded(All(FillContainer30(6), Crate(500, 40, size: 2)));

        var done = repair.Apply(multiple: 1, moveFirst: ["Iron"]);

        Assert.Equal((2, 1, 1), (done.MovedCount, done.DestinationCount, done.ItemCount));
        Assert.Equal("1003,1004", IdsOf(workspace, 40));
        Assert.Equal("1000,1001,1002", IdsOf(workspace, 30));
        Assert.Contains(workspace.Current!.WorldObjects, w => w.Id == 1003); // moved, not deleted
        Assert.DoesNotContain(workspace.Current!.WorldObjects, w => w.Id == 1005);
    }

    [Fact]
    public void Plan_WithNothingTicked_MovesNothing()
    {
        var (repair, _) = CreateLoaded(All(FillContainer30(6), Crate(500, 40, size: 10)));

        var plan = repair.Plan(multiple: 1);

        Assert.Equal((0, 3), (plan.MovedCount, plan.ItemCount));
        Assert.Equal(10, plan.FreeSlots);
    }

    [Fact]
    public void OnlyStorageCrates_AreDestinations()
    {
        // Alice's equipment (11) and the orphan inventory (99) have room, but they are not storage.
        var (repair, workspace) = CreateLoaded(FillContainer30(4));

        var done = repair.Apply(multiple: 1, moveFirst: ["Iron"]);

        Assert.Equal((0, 1), (done.MovedCount, done.ItemCount));
        Assert.Equal("", IdsOf(workspace, 11));
    }

    [Fact]
    public void ACrateOnAnotherPlanet_IsNotADestination()
    {
        var (repair, _) = CreateLoaded(All(FillContainer30(4), Crate(500, 40, size: 5, planet: 12345)));

        Assert.Equal(0, repair.Plan(multiple: 1, moveFirst: ["Iron"]).MovedCount);
    }

    [Fact]
    public void ALogisticsCrate_IsNotADestination()
    {
        // Drones would carry the items straight back out.
        var (repair, _) = CreateLoaded(All(FillContainer30(4), Crate(500, 40, size: 5, logistics: true)));

        Assert.Equal(0, repair.Plan(multiple: 1, moveFirst: ["Iron"]).MovedCount);
    }

    [Fact]
    public void AMapPlacedCrate_IsNotADestination()
    {
        // One of the developers' crates (its real id), on the same "no planet" as the source, so
        // only its origin keeps it out: items go into storage the player built.
        var (repair, _) = CreateLoaded(All(FillContainer30(4), Crate(101_464_942, 40, size: 5)));

        Assert.Equal(0, repair.Plan(multiple: 1, moveFirst: ["Iron"]).MovedCount);
    }

    [Fact]
    public void ACrateAlreadyHoldingTheType_IsFilledFirst()
    {
        // 41 is emptier, but 42 already holds Iron: stacks stay together.
        var (repair, workspace) = CreateLoaded(All(
            FillContainer30(5),
            Crate(500, 41, size: 10),
            save => save.WorldObjects.Add(new WorldObject { Id = 900, GId = "Iron" }),
            Crate(501, 42, size: 3, contents: "900")));

        repair.Apply(multiple: 1, moveFirst: ["Iron"]);

        Assert.Equal("900,1003,1004", IdsOf(workspace, 42));
        Assert.Equal("", IdsOf(workspace, 41));
    }

    [Fact]
    public void ThenTheCrateWithTheMostFreeSlots()
    {
        var (repair, workspace) = CreateLoaded(All(FillContainer30(5), Crate(500, 41, size: 2), Crate(501, 42, size: 6)));

        repair.Apply(multiple: 1, moveFirst: ["Iron"]);

        Assert.Equal("1003,1004", IdsOf(workspace, 42));
    }

    [Fact]
    public void TheTickedOrder_DecidesWhoGetsScarceSpace()
    {
        // Five items in three slots leaves 2000 (Cobalt) and 1003 (Iron) over, and there is one free
        // slot. Cobalt is ticked first, so Cobalt moves and the Iron goes.
        var (repair, workspace) = CreateLoaded(All(
            FillContainer30(4),
            save =>
            {
                Cobalt(save, 2000);
                var index = save.Inventories.FindIndex(i => i.Id == 30);
                save.Inventories[index] = save.Inventories[index] with { WorldObjectIds = "1000,1001,1002,2000,1003" };
            },
            Crate(500, 40, size: 1)));

        var done = repair.Apply(multiple: 1, moveFirst: ["Cobalt", "Iron"]);

        Assert.Equal("2000", IdsOf(workspace, 40));
        Assert.Equal((1, 1), (done.MovedCount, done.ItemCount));
        Assert.DoesNotContain(workspace.Current!.WorldObjects, w => w.Id == 1003);
    }

    [Fact]
    public void AnItemFromABackpack_UsesItsPlayersPlanet()
    {
        // Bob is on Prime; a crate on Prime takes his overflow, so his backpack is valid again.
        var (repair, workspace) = CreateLoaded(All(
            WorkspaceFixtures.OverFillBobsInventory,
            Crate(500, 40, size: 5, planet: PCEdit.SaveFileHandler.PlanetHash.Of("Prime"))));

        var done = repair.Apply(multiple: 1, moveFirst: ["Item201"]);

        Assert.Equal((1, 0), (done.MovedCount, done.ItemCount));
        Assert.Equal("201", IdsOf(workspace, 40));
    }

    [Fact]
    public void AMultipleBelowOne_IsRejected()
    {
        var (repair, _) = CreateLoaded();

        Assert.Throws<ArgumentOutOfRangeException>(() => repair.Plan(multiple: 0));
    }
}
