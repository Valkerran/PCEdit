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

    private static InventoriesViewModel CreateLoaded(
        Action<PCEdit.SaveFileHandler.Models.PlanetCrafterSaveFile>? adjust = null,
        FakeNavigationService? navigation = null)
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        adjust?.Invoke(save);
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var itemCatalog = new ItemCatalog();
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace)), navigation ?? new FakeNavigationService(), localizer);
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
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var itemCatalog = new ItemCatalog();
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace)), new FakeNavigationService(), localizer);
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
    public void IdQuery_MatchesInventoryIdsAndContainedItemIdsByPrefix()
    {
        var vm = CreateLoaded();

        // Inventory 20 by its own id; 10 holds items 200/201 and 30 holds item 202. The item
        // names ("Item200"...) also contain "20", but an id search does not match names.
        vm.Query = "20";

        Assert.Equal([10, 20, 30], vm.Groups.Select(g => g.InventoryId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void IdQuery_MatchesTheOwningContainersObjectId()
    {
        var vm = CreateLoaded();

        // Inventories 10 and 11 by id; inventory 30 through its container, world object 100.
        vm.Query = "#1";

        Assert.Equal([10, 11, 30], vm.Groups.Select(g => g.InventoryId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void IdQuery_ForAFullItemId_FindsItsInventory()
    {
        var vm = CreateLoaded();

        vm.Query = "202";

        Assert.Equal([30], vm.Groups.Select(g => g.InventoryId).ToArray());
    }

    [Fact]
    public void ItemQuery_NarrowsTheCardToTheMatchingItem()
    {
        var vm = CreateLoaded();

        vm.Query = "item201"; // inventory 10 holds items 200 and 201

        var card = Assert.Single(vm.Groups);
        Assert.Equal(10, card.InventoryId);
        Assert.Equal([201], card.Items.Select(i => i.WorldObjectId).ToArray());
        Assert.Equal("2/5", card.CapacityLabel);
    }

    [Fact]
    public void InventoryQuery_ShowsTheWholeCard()
    {
        var vm = CreateLoaded();

        vm.Query = "alice"; // matches the label of inventory 10 itself

        var card = vm.Groups.Single(g => g.InventoryId == 10);
        Assert.Equal([200, 201], card.Items.Select(i => i.WorldObjectId).ToArray());
        Assert.False(card.IsNarrowed);
    }

    [Fact]
    public void ClearingTheQuery_RestoresEveryItem()
    {
        var vm = CreateLoaded();
        vm.Query = "item201";

        vm.Query = string.Empty;

        Assert.Equal(2, vm.Groups.Single(g => g.InventoryId == 10).Items.Count);
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
    public void NeedsAttentionFilter_ShowsOnlyTheFlaggedInventories()
    {
        var vm = CreateLoaded(WorkspaceFixtures.OverFillBobsInventory);

        vm.Filter = InventoryFilter.NeedsAttention;

        Assert.Equal([20], vm.Groups.Select(g => g.InventoryId).ToArray());
    }

    [Fact]
    public void AttentionCount_CountsFlaggedInventories_WhateverTheCurrentFilter()
    {
        var vm = CreateLoaded(WorkspaceFixtures.OverFillBobsInventory);

        vm.Filter = InventoryFilter.Equipment;
        vm.Query = "zzz-nothing";

        Assert.Equal(1, vm.AttentionCount);
    }

    [Fact]
    public void Load_OfASaveWithNothingFlagged_DropsANeedsAttentionFilter()
    {
        // The chip hides when nothing is flagged, so a filter left on it from the previous save
        // would leave an empty page with no visible way out.
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var itemCatalog = new ItemCatalog();
        var clean = new InventoriesViewModel(workspace, new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace)), new FakeNavigationService(), localizer)
        {
            Filter = InventoryFilter.NeedsAttention,
        };

        clean.Load();

        Assert.Equal(InventoryFilter.All, clean.Filter);
        Assert.Equal(6, clean.Groups.Count);
    }

    [Fact]
    public void AttentionCount_IsZeroForAnUnmoddedSave()
    {
        Assert.Equal(0, CreateLoaded().AttentionCount);
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
    public async Task MoveStackCommand_OpensTheMoveDialogForTheWholeStack()
    {
        var nav = new FakeNavigationService();
        var vm = CreateLoaded(navigation: nav);
        var stack = new InventoryStackView("Iron", "Iron", "ore.png", [4022, 5100]);

        await vm.MoveStackCommand.ExecuteAsync(stack);

        Assert.Equal([4022, 5100], Assert.Single(nav.SelectInventoryRequests));
    }

    [Fact]
    public void OpenFileCommand_Navigates()
    {
        var nav = new FakeNavigationService();
        var store = new FakeSaveFileStore();
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        var itemCatalog = new ItemCatalog();
        var vm = new InventoriesViewModel(workspace, new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace)), nav, localizer);

        vm.OpenFileCommand.Execute(null);

        Assert.Equal(1, nav.OpenFileCount);
    }
}
