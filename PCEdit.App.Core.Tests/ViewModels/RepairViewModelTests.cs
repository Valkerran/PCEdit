using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class RepairViewModelTests
{
    private static readonly string Original = Path.Combine("saves", "Standard-2.json");
    private static readonly string Copy = Path.Combine("saves", "Standard-3.json");

    private sealed record Rig(
        RepairViewModel Vm,
        SaveFileWorkspace Workspace,
        FakeSaveFileStore Store,
        FakeFilePickerService Picker,
        FakeNavigationService Navigation,
        FakeDialogService Dialogs,
        FakeScreenReaderAnnouncer Announcer);

    // Bob's one-slot inventory (20) holding two items: one to remove at 1x.
    private static Rig Create(
        Action<SaveFileWorkspace>? beforeOpening = null,
        Action<PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile>? adjust = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        WorkspaceFixtures.OverFillBobsInventory(save);
        adjust?.Invoke(save);
        store.Seed(Original, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Original);
        beforeOpening?.Invoke(workspace);
        var picker = new FakeFilePickerService { SaveCopyResult = Copy };
        var navigation = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        var announcer = new FakeScreenReaderAnnouncer();
        var vm = new RepairViewModel(new OverflowRepair(workspace, new ItemCatalog()), workspace, picker, announcer, navigation, dialogs, localizer);
        vm.Initialize();
        return new Rig(vm, workspace, store, picker, navigation, dialogs, announcer);
    }

    private static string IdsOf(SaveFileWorkspace workspace, int inventoryId) =>
        workspace.Current!.Inventories.Single(i => i.Id == inventoryId).WorldObjectIds;

    [Fact]
    public void Opening_ShowsWhatTheRepairWouldRemove()
    {
        var rig = Create();

        Assert.Contains("1", rig.Vm.PreviewText);
        Assert.True(rig.Vm.CanRepair);
    }

    // A Storage Crate on Prime, where Bob is, with room for his overflow.
    private static void CrateOnPrime(PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile save)
    {
        save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject
        {
            Id = 500, GId = "Container1", LinkedInventoryId = 40, Planet = PCEdit.SaveFileHandler.PlanetHash.Of("Prime"),
        });
        save.Inventories.Add(new PCEdit.SaveFileHandler.Models.Inventory { Id = 40, WorldObjectIds = "", Size = 5 });
    }

    // Alice's two-slot equipment (11) holding four items: two more types over capacity.
    private static void OverFillAlicesEquipment(PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile save)
    {
        save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 310, GId = "Iron" });
        save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 311, GId = "Cobalt" });
        save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 312, GId = "Iron" });
        save.WorldObjects.Add(new PCEdit.SaveFileHandler.Models.WorldObject { Id = 313, GId = "Cobalt" });
        var index = save.Inventories.FindIndex(i => i.Id == 11);
        save.Inventories[index] = save.Inventories[index] with { WorldObjectIds = "310,311,312,313" };
    }

    [Fact]
    public void Opening_ListsEachTypeOverCapacity_NoneTicked()
    {
        var rig = Create();

        var choice = Assert.Single(rig.Vm.PriorityTypes);
        Assert.Equal("Item201", choice.GId);
        Assert.False(choice.IsChecked);
        Assert.Null(rig.Vm.MovedText);
    }

    [Fact]
    public void TickingAType_PreviewsItMovingIntoFreeStorage()
    {
        var rig = Create(adjust: CrateOnPrime);

        rig.Vm.PriorityTypes.Single().IsChecked = true;

        Assert.NotNull(rig.Vm.MovedText);
        Assert.Contains("1", rig.Vm.MovedText);
        Assert.Contains("5", rig.Vm.FreeSlotsText); // the crate's five slots
        Assert.True(rig.Vm.CanRepair); // nothing left to remove, but there is still a move to make
    }

    [Fact]
    public void MovingATypeDown_ChangesItsPriority()
    {
        var rig = Create(adjust: OverFillAlicesEquipment);
        var first = rig.Vm.PriorityTypes[0];

        rig.Vm.MoveDownCommand.Execute(first);

        Assert.Same(first, rig.Vm.PriorityTypes[1]);
        rig.Vm.MoveUpCommand.Execute(first);
        Assert.Same(first, rig.Vm.PriorityTypes[0]);
    }

    [Fact]
    public void ChangingTheMultiple_KeepsTheTicks()
    {
        var rig = Create(adjust: OverFillAlicesEquipment);
        var ticked = rig.Vm.PriorityTypes.First(c => c.GId == "Cobalt");
        ticked.IsChecked = true;

        rig.Vm.Multiple = 2; // Alice's four items now fit in 2 x 2 slots; Bob's two in 2 x 1

        Assert.Empty(rig.Vm.PriorityTypes);
        rig.Vm.Multiple = 1;
        Assert.True(rig.Vm.PriorityTypes.First(c => c.GId == "Cobalt").IsChecked);
    }

    [Fact]
    public async Task Repair_MovesTheTickedTypes_IntoTheCopy()
    {
        var rig = Create(adjust: CrateOnPrime);
        rig.Vm.PriorityTypes.Single().IsChecked = true;

        await rig.Vm.RepairCommand.ExecuteAsync(null);

        Assert.Equal("201", IdsOf(rig.Workspace, 40));
        Assert.Equal("200", IdsOf(rig.Workspace, 20));
        Assert.Contains(rig.Workspace.Current!.WorldObjects, w => w.Id == 201);
        Assert.Contains("1", Assert.Single(rig.Announcer.Announcements));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 100)]
    public void Multiple_StaysBetweenOneAndAHundred(int typed, int kept)
    {
        var rig = Create();

        rig.Vm.Multiple = typed;

        Assert.Equal(kept, rig.Vm.Multiple);
    }

    [Fact]
    public void AMultipleThatRemovesNothing_DisablesTheRepair()
    {
        var rig = Create();

        rig.Vm.Multiple = 2; // two items in one slot fit 2x

        Assert.False(rig.Vm.CanRepair);
        Assert.Empty(rig.Vm.PriorityTypes);
    }

    [Fact]
    public void WithUnsavedChanges_TheRepairIsUnavailable()
    {
        // A failed copy is undone by reloading the original, which would also throw these away.
        var rig = Create(workspace => workspace.GrantTerraTokens(1, 5));

        Assert.True(rig.Vm.HasUnsavedChanges);
        Assert.False(rig.Vm.CanRepair);
    }

    [Fact]
    public async Task Repair_WritesARepairedCopy_AndNeverWritesTheOriginal()
    {
        var rig = Create();

        await rig.Vm.RepairCommand.ExecuteAsync(null);

        Assert.Equal([(Original, Copy)], rig.Store.Copies);
        Assert.Equal(0, rig.Store.SaveCallCount);
        Assert.Equal(Copy, rig.Workspace.FilePath);
        Assert.Equal("200", IdsOf(rig.Workspace, 20));
        Assert.False(rig.Workspace.IsDirty);
        Assert.Single(rig.Announcer.Announcements);
        Assert.Equal(1, rig.Navigation.CloseModalCount);
    }

    [Fact]
    public async Task Repair_SuggestsAFileBesideTheOriginal()
    {
        var rig = Create();

        await rig.Vm.RepairCommand.ExecuteAsync(null);

        var suggested = Assert.Single(rig.Picker.SuggestedPaths);
        Assert.NotEqual(Original, suggested);
        Assert.Equal(Path.GetDirectoryName(Original), Path.GetDirectoryName(suggested));
    }

    [Fact]
    public async Task Repair_WhenThePickerIsCancelled_ChangesNothing()
    {
        var rig = Create();
        rig.Picker.SaveCopyResult = null;

        await rig.Vm.RepairCommand.ExecuteAsync(null);

        Assert.Equal("200,201", IdsOf(rig.Workspace, 20));
        Assert.False(rig.Workspace.IsDirty);
        Assert.Empty(rig.Store.Copies);
        Assert.Equal(0, rig.Navigation.CloseModalCount);
    }

    [Fact]
    public async Task Repair_WhenTheCopyCannotBeWritten_ReopensTheOriginal_AndSaysSo()
    {
        var rig = Create();
        rig.Store.FailSaveCopy = true;
        var loadsBefore = rig.Store.LoadCallCount;

        await rig.Vm.RepairCommand.ExecuteAsync(null);

        Assert.Equal(loadsBefore + 1, rig.Store.LoadCallCount); // the repair undone from disk
        Assert.Equal(Original, rig.Workspace.FilePath);
        Assert.False(rig.Workspace.IsDirty);
        Assert.Single(rig.Dialogs.Errors);
        Assert.Equal(0, rig.Navigation.CloseModalCount);
    }
}
