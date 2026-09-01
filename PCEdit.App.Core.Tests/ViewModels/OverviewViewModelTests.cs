using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class OverviewViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    private static OverviewViewModel CreateLoaded(FakeNavigationService? nav = null)
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var vm = new OverviewViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, nav ?? new FakeNavigationService());
        vm.Load();
        return vm;
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
        var workspace = new SaveFileWorkspace(new FakeSaveFileStore(), new FakeScreenReaderAnnouncer(), localizer);
        var vm = new OverviewViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService());

        vm.Load();

        Assert.Null(vm.GameVersionText);
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
