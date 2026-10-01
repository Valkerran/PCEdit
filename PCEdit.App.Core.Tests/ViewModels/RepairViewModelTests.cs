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
    private static Rig Create(Action<SaveFileWorkspace>? beforeOpening = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        WorkspaceFixtures.OverFillBobsInventory(save);
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
        Assert.Single(rig.Vm.TopTypes);
        Assert.True(rig.Vm.CanRepair);
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
        Assert.Empty(rig.Vm.TopTypes);
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
