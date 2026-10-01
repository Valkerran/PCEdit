using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class OverviewViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    private static OverviewViewModel CreateLoaded(
        FakeNavigationService? nav = null,
        Action<PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile>? adjust = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        adjust?.Invoke(save);
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = Create(workspace, localizer, nav ?? new FakeNavigationService());
        vm.Load();
        return vm;
    }

    private static OverviewViewModel Create(SaveFileWorkspace workspace, Localizer localizer, FakeNavigationService nav)
    {
        var itemCatalog = new ItemCatalog();
        var editor = new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace));
        return new OverviewViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, nav, editor);
    }

    [Fact]
    public void Load_BuildsOneRowPerPlayer_WithFormattedLocationAndProgress()
    {
        var vm = CreateLoaded();

        Assert.Equal(2, vm.Players.Count);
        var alice = vm.Players.First(p => p.Name == "Alice");
        Assert.Contains("Prime", alice.LocationText);
        Assert.Contains("0,0,0", alice.LocationText);
        Assert.Contains("objects crafted", alice.ProgressText);
        Assert.Contains("terra tokens earned", alice.ProgressText);
    }

    [Fact]
    public void Load_SurfacesTheGameVersionThatWroteTheSave()
    {
        var vm = CreateLoaded();

        Assert.NotNull(vm.GameVersionText);
        Assert.Contains("2.102", vm.GameVersionText);
    }

    [Fact]
    public void Load_WithNoSaveLoaded_ClearsTheGameVersion()
    {
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(new FakeSaveFileStore(), new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        var vm = Create(workspace, localizer, new FakeNavigationService());

        vm.Load();

        Assert.Null(vm.GameVersionText);
    }
    [Fact]
    public void Load_OfAnUnmoddedSave_ShowsNoAttentionBanner()
    {
        var vm = CreateLoaded();

        Assert.False(vm.HasAttention);
        Assert.Null(vm.OverCapacityText);
        Assert.Null(vm.NearLoadLimitText);
    }

    [Fact]
    public void Load_OfASaveWithAnOverFullContainer_SaysHowManyThereAre()
    {
        var vm = CreateLoaded(adjust: WorkspaceFixtures.OverFillBobsInventory);

        Assert.True(vm.HasAttention);
        Assert.NotNull(vm.OverCapacityText);
        Assert.Contains("1", vm.OverCapacityText);
        Assert.Null(vm.NearLoadLimitText); // two items are nowhere near the game's load cap
    }

    [Fact]
    public void Load_OfASaveWithAContainerNearTheGamesLoadCap_AddsTheLoadLimitWarning()
    {
        var vm = CreateLoaded(adjust: save =>
        {
            // 7,201 items in the three-slot storage container (inventory 30): past 90% of 8,000.
            var ids = Enumerable.Range(10_000, 7_201).ToList();
            save.WorldObjects.AddRange(ids.Select(id => new PCEdit.SaveFileHandler.Models.WorldObject { Id = id, GId = "Iron" }));
            var index = save.Inventories.FindIndex(i => i.Id == 30);
            save.Inventories[index] = save.Inventories[index] with { WorldObjectIds = string.Join(',', ids) };
        });

        Assert.True(vm.HasAttention);
        Assert.NotNull(vm.NearLoadLimitText);
        Assert.Contains("1", vm.NearLoadLimitText);
    }

    [Fact]
    public async Task ShowAttentionCommand_OpensTheInventoriesNeedingAttention()
    {
        var nav = new FakeNavigationService();
        var vm = CreateLoaded(nav, WorkspaceFixtures.OverFillBobsInventory);

        await vm.ShowAttentionCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.InventoriesNeedingAttentionCount);
    }

    [Fact]
    public async Task RepairCommand_OpensTheRepair()
    {
        var nav = new FakeNavigationService();
        var vm = CreateLoaded(nav, WorkspaceFixtures.OverFillBobsInventory);

        await vm.RepairCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.RepairCount);
    }

    [Fact]
    public void OpenFileCommand_Navigates()
    {
        var nav = new FakeNavigationService();
        var vm = CreateLoaded(nav);

        vm.OpenFileCommand.Execute(null);

        Assert.Equal(1, nav.OpenFileCount);
    }
}
