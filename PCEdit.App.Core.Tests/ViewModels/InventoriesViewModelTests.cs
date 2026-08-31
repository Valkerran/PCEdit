using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class InventoriesViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    private static InventoriesViewModel CreateLoaded()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, new ItemCatalog(), new LogisticsGroupCatalog(), localizer, new PlanetIndex(workspace)), new FakeNavigationService(), localizer);
        vm.Load();
        return vm;
    }

    [Fact]
    public void Load_ShowsEveryInventory()
    {
        var vm = CreateLoaded();

        Assert.Equal(6, vm.Groups.Count);
        Assert.False(vm.IsFilteredEmpty);
    }

    [Fact]
    public void Query_MatchesInventoryLabel()
    {
        var vm = CreateLoaded();

        vm.Query = "alice";

        Assert.Equal([10, 11], vm.Groups.Select(g => g.InventoryId).OrderBy(id => id).ToArray());
    }

    private static InventoriesViewModel CreateLoadedMultiWorld()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.CreateMultiWorld());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, new ItemCatalog(), new LogisticsGroupCatalog(), localizer, new PlanetIndex(workspace)), new FakeNavigationService(), localizer);
        vm.Load();
        return vm;
    }

    [Fact]
    public void SingleWorldSave_HidesTheWorldFilter()
    {
        var vm = CreateLoaded();

        Assert.False(vm.ShowWorldFilter);
    }

    [Fact]
    public void MultiWorldSave_ShowsTheWorldFilter_AndNarrowsToTheSelectedWorld()
    {
        var vm = CreateLoadedMultiWorld();

        Assert.True(vm.ShowWorldFilter);

        vm.SelectedWorld = vm.WorldOptions.Single(o => o.PlanetId == "Aqualis");

        Assert.Equal([20, 21, 30], vm.Groups.Select(g => g.InventoryId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void UnknownWorldOption_NarrowsToInventoriesWithNoResolvableWorld()
    {
        var vm = CreateLoadedMultiWorld();

        vm.SelectedWorld = vm.WorldOptions.Single(o => o is { IsAll: false, PlanetId: null });

        Assert.Equal([99], vm.Groups.Select(g => g.InventoryId).ToArray());
    }

    [Fact]
    public void Query_MatchesContainedItemName()
    {
        var vm = CreateLoaded();

        vm.Query = "item202"; // lives only in container inventory 30

        Assert.Equal([30], vm.Groups.Select(g => g.InventoryId).ToArray());
    }

    [Fact]
    public void Filter_NarrowsByKind()
    {
        var vm = CreateLoaded();

        vm.Filter = InventoryFilter.Equipment;

        Assert.All(vm.Groups, g => Assert.Equal(InventoryKind.Equipment, g.Kind));
        Assert.Equal([11, 21], vm.Groups.Select(g => g.InventoryId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void QueryWithNoMatch_ReportsFilteredEmpty()
    {
        var vm = CreateLoaded();

        vm.Query = "zzz-nothing";

        Assert.Empty(vm.Groups);
        Assert.True(vm.IsFilteredEmpty);
    }

    [Fact]
    public void OpenFileCommand_Navigates()
    {
        var nav = new FakeNavigationService();
        var store = new FakeSaveFileStore();
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, new ItemCatalog(), new LogisticsGroupCatalog(), localizer, new PlanetIndex(workspace)), nav, localizer);

        vm.OpenFileCommand.Execute(null);

        Assert.Equal(1, nav.OpenFileCount);
    }
}
