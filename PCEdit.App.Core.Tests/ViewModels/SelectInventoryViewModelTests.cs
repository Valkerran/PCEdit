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
    private static SelectInventoryViewModel CreateForItem200()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var itemCatalog = new ItemCatalog();
        var editor = new InventoryEditor(workspace, itemCatalog, new LogisticsGroupCatalog(itemCatalog), localizer, new PlanetIndex(workspace));
        var vm = new SelectInventoryViewModel(editor, new FakeScreenReaderAnnouncer(), new FakeNavigationService(), new FakeDialogService(), localizer);
        vm.Initialize(200);
        return vm;
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
}
