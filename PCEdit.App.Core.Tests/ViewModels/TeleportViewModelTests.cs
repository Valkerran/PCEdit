using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;
using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class TeleportViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    private static TeleportViewModel CreateLoaded(bool multiWorld)
    {
        var save = multiWorld ? WorkspaceFixtures.CreateMultiWorld() : WorkspaceFixtures.Create();
        save.WorldObjects.Add(new WorldObject { Id = 500, GId = "EscapePod", Position = "1,2,3", Planet = PlanetHash.Of("Prime") });
        save.WorldObjects.Add(new WorldObject { Id = 501, GId = "Teleporter1", Position = "4,5,6", Planet = multiWorld ? PlanetHash.Of("Aqualis") : PlanetHash.Of("Prime") });
        save.WorldObjects.Add(new WorldObject { Id = 502, GId = "podUnplaced", Position = "7,8,9" });

        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var vm = new TeleportViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(workspace));
        vm.Load();
        return vm;
    }

    [Fact]
    public void Landmarks_CarryTheirResolvedPlanetId()
    {
        var vm = CreateLoaded(multiWorld: true);
        vm.ShowAllWorldLandmarks = true;

        var byId = vm.Landmarks.ToDictionary(l => l.WorldObjectId);

        Assert.Equal("Prime", byId[500].PlanetId);
        Assert.Equal("Aqualis", byId[501].PlanetId);
        Assert.Null(byId[502].PlanetId);
        Assert.Equal("Aqualis", byId[501].PlanetHint);
    }

    [Fact]
    public void Landmarks_AreTrimmedToTheSelectedDestinationPlanet()
    {
        var vm = CreateLoaded(multiWorld: true);

        vm.SelectedPlanetId = "Aqualis";

        Assert.Equal([501], vm.Landmarks.Select(l => l.WorldObjectId).ToArray());
    }

    [Fact]
    public void ShowAllWorldLandmarks_RestoresTheFullList()
    {
        var vm = CreateLoaded(multiWorld: true);
        vm.SelectedPlanetId = "Aqualis";

        vm.ShowAllWorldLandmarks = true;

        Assert.Equal([500, 501, 502], vm.Landmarks.Select(l => l.WorldObjectId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void SingleWorldSave_HidesTheFilterAndShowsEveryLandmark()
    {
        var vm = CreateLoaded(multiWorld: false);

        Assert.False(vm.ShowWorldLandmarkFilter);
        Assert.Equal([500, 501, 502], vm.Landmarks.Select(l => l.WorldObjectId).OrderBy(id => id).ToArray());
    }
}
