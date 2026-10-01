using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class SelectInventoryViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    // Item 200 lives in inventory 10, so the destinations are 11, 20, 21, 30 (container,
    // world object 100) and 99 (unowned).
    private static SelectInventoryViewModel CreateForItem200() => CreateFor([200]).Vm;

    // Alice's inventory (10) holds 200 and 201: as a stack, both of them.
    private static (SelectInventoryViewModel Vm, SaveFileWorkspace Workspace, FakeScreenReaderAnnouncer Announcer) CreateFor(
        int[] stack,
        Action<PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile>? adjust = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        adjust?.Invoke(save);
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var itemCatalog = new ItemCatalog();
        var editor = new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace));
        var announcer = new FakeScreenReaderAnnouncer();
        var vm = new SelectInventoryViewModel(editor, announcer, new FakeNavigationService(), new FakeDialogService(), localizer);
        vm.Initialize(stack);
        return (vm, workspace, announcer);
    }

    private static int[] Shown(SelectInventoryViewModel vm) =>
        vm.FilteredOptions.Select(o => o.InventoryId).OrderBy(id => id).ToArray();

    [Fact]
    public void EmptyQuery_ShowsEveryDestination()
    {
        Assert.Equal([11, 20, 21, 30, 99], Shown(CreateForItem200()));
    }

    [Fact]
    public void TextQuery_MatchesTheLabel()
    {
        var vm = CreateForItem200();

        vm.Query = "alice"; // Alice owns 10 (the source, excluded) and 11

        Assert.Equal([11], Shown(vm));
    }

    [Fact]
    public void IdQuery_MatchesInventoryIdsByPrefix()
    {
        var vm = CreateForItem200();

        vm.Query = "2";

        Assert.Equal([20, 21], Shown(vm));
    }

    [Fact]
    public void IdQuery_MatchesTheOwningContainersObjectId()
    {
        var vm = CreateForItem200();

        // 11 by its own id; 30 through its container, world object 100.
        vm.Query = "#1";

        Assert.Equal([11, 30], Shown(vm));
    }

    [Fact]
    public void IdQuery_DoesNotFallBackToTheLabelText()
    {
        var vm = CreateForItem200();

        // Container 30's label ends "(Object #100)", so "00" is in its label text - but it is not
        // the prefix of any id, and an id search does not fall back to the label.
        vm.Query = "00";

        Assert.Empty(vm.FilteredOptions);
    }

    [Fact]
    public void AStack_StartsAtOne_AndAllowsUpToTheWholeStack()
    {
        var (vm, _, _) = CreateFor([200, 201]);

        Assert.True(vm.IsStack);
        Assert.Equal(1, vm.Quantity);
        Assert.Equal(2, vm.MaxQuantity);
    }

    [Fact]
    public void ASingleItem_HasNoQuantityToChoose()
    {
        Assert.False(CreateForItem200().IsStack);
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    public void Quantity_StaysWithinTheStack(int typed, int kept)
    {
        var (vm, _, _) = CreateFor([200, 201]);

        vm.Quantity = typed;

        Assert.Equal(kept, vm.Quantity);
    }

    [Fact]
    public void AllCommand_TakesTheWholeStack()
    {
        var (vm, _, _) = CreateFor([200, 201]);

        vm.AllCommand.Execute(null);

        Assert.Equal(2, vm.Quantity);
    }

    [Fact]
    public void ADestinationWithoutRoomForTheQuantity_IsDisabled()
    {
        var (vm, _, _) = CreateFor([200, 201]);

        vm.Quantity = 2;

        var room = vm.FilteredOptions.ToDictionary(o => o.InventoryId, o => o.HasRoom);
        Assert.False(room[20]); // Bob's one-slot inventory
        Assert.True(room[30]);  // holds 1 of 3
        Assert.True(room[11]);  // 2 empty slots
    }

    [Fact]
    public async Task Select_MovesTheLastItemsOfTheStack()
    {
        var (vm, workspace, _) = CreateFor([200, 201]);

        await vm.SelectCommand.ExecuteAsync(vm.FilteredOptions.Single(o => o.InventoryId == 30));

        Assert.Equal("200", workspace.Current!.Inventories.Single(i => i.Id == 10).WorldObjectIds);
        Assert.Equal("202,201", workspace.Current!.Inventories.Single(i => i.Id == 30).WorldObjectIds);
    }

    [Fact]
    public async Task Select_OfSeveralItems_AnnouncesHowMany()
    {
        var (vm, _, announcer) = CreateFor([200, 201]);
        vm.Quantity = 2;

        await vm.SelectCommand.ExecuteAsync(vm.FilteredOptions.Single(o => o.InventoryId == 30));

        var said = Assert.Single(announcer.Announcements);
        Assert.Contains("2", said);
    }

    private static void SetIdList(PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile save, int inventoryId, string csv)
    {
        var index = save.Inventories.FindIndex(i => i.Id == inventoryId);
        save.Inventories[index] = save.Inventories[index] with { WorldObjectIds = csv };
    }

    [Fact]
    public void FullAndOverFullDestinations_AreNotOffered()
    {
        var (vm, _, _) = CreateFor([200], save =>
        {
            save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 300, GId = "Iron" });
            save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 301, GId = "Iron" });
            save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 302, GId = "Iron" });
            SetIdList(save, 99, "300");         // 1/1: at capacity
            SetIdList(save, 21, "301,302,202"); // 3/2: over capacity, as a stacking mod leaves it
            SetIdList(save, 30, "");
        });

        Assert.Equal([11, 20, 30], Shown(vm));
        Assert.False(vm.HasNoDestinations);
    }

    [Fact]
    public void WhenEveryOtherInventoryIsFull_SaysThereIsNowhereToMove()
    {
        var (vm, _, _) = CreateFor([200], save =>
        {
            foreach (var inventory in save.Inventories.Where(i => i.Id != 10).ToList())
            {
                var index = save.Inventories.IndexOf(inventory);
                save.Inventories[index] = inventory with { Size = 0 };
            }
        });

        Assert.Empty(vm.FilteredOptions);
        Assert.True(vm.HasNoDestinations);
    }
}
