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
        if (multiWorld)
        {
            save.WorldObjects.Add(new WorldObject { Id = 503, GId = "EscapePodInterplanetary", Position = "42,0,42", Planet = PlanetHash.Of("Aqualis") });
            save.Terraformations.Add(new PlanetTerraformation { PlanetId = "Selenea" }); // a world with no landmarks
        }

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

        Assert.Equal([501, 503], vm.Landmarks.Select(l => l.WorldObjectId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void ShowAllWorldLandmarks_RestoresTheFullList()
    {
        var vm = CreateLoaded(multiWorld: true);
        vm.SelectedPlanetId = "Aqualis";

        vm.ShowAllWorldLandmarks = true;

        Assert.Equal([500, 501, 502, 503], vm.Landmarks.Select(l => l.WorldObjectId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void ChoosingADifferentWorld_AimsCoordinatesAtThatWorldsArrivalPoint()
    {
        var vm = CreateLoaded(multiWorld: true); // Alice starts on "Prime" at 0,0,0

        vm.SelectedPlanetId = "Aqualis";

        // prefers EscapePodInterplanetary (503 @ 42,0,42) over the Aqualis teleporter (501 @ 4,5,6)
        Assert.Equal(("42", "0", "42"), (vm.X, vm.Y, vm.Z));

        vm.SelectedPlanetId = "Prime"; // back to Alice's own world -> her real position
        Assert.Equal(("0", "0", "0"), (vm.X, vm.Y, vm.Z));
    }

    [Fact]
    public void ChoosingADifferentWorldWithNoLandmark_LeavesCoordinatesAlone()
    {
        var vm = CreateLoaded(multiWorld: true);
        vm.X = "5"; vm.Y = "6"; vm.Z = "7";

        vm.SelectedPlanetId = "Selenea"; // no landmarks on Selenea in this fixture

        Assert.Equal(("5", "6", "7"), (vm.X, vm.Y, vm.Z));
    }

    [Fact]
    public void UseCurrentPosition_ResetsTheWorldAndCoordinatesToThePlayer()
    {
        var vm = CreateLoaded(multiWorld: true); // player Alice starts on "Prime"
        vm.SelectedPlanetId = "Aqualis";
        vm.X = "999";

        vm.UseCurrentPositionCommand.Execute(null);

        Assert.Equal("Prime", vm.SelectedPlanetId);
        Assert.Equal("0", vm.X);
    }

    [Fact]
    public void SingleWorldSave_HidesTheFilterAndShowsEveryLandmark()
    {
        var vm = CreateLoaded(multiWorld: false);

        Assert.False(vm.ShowWorldLandmarkFilter);
        Assert.Equal([500, 501, 502], vm.Landmarks.Select(l => l.WorldObjectId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void MultiplayerSave_EditsOnlyTheSelectedPlayer_AndFormFollowsThePlayer()
    {
        // Alice (id 1) on Prime, Bob (id 2) on Aqualis.
        var save = WorkspaceFixtures.CreateMultiWorld();
        var ai = save.Players.FindIndex(p => p.Name == "Alice");
        var bi = save.Players.FindIndex(p => p.Name == "Bob");
        save.Players[ai] = save.Players[ai] with { PlayerPosition = "1,1,1" };
        save.Players[bi] = save.Players[bi] with { PlayerPosition = "2,2,2" };

        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var ws = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        ws.Load(Path);
        var vm = new TeleportViewModel(ws, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(ws));
        vm.Load();

        // Form starts on the first player.
        Assert.Equal("Alice", vm.SelectedPlayer!.Name);
        Assert.Equal("Prime", vm.SelectedPlanetId);
        Assert.Equal(("1", "1", "1"), (vm.X, vm.Y, vm.Z));

        // Switch to Bob — the whole form re-initialises from Bob.
        vm.SelectedPlayer = vm.Players.Single(p => p.Name == "Bob");
        Assert.Equal("Aqualis", vm.SelectedPlanetId);
        Assert.Equal(("2", "2", "2"), (vm.X, vm.Y, vm.Z));

        // Teleport Bob to Prime.
        vm.SelectedPlanetId = "Prime";
        vm.X = "9"; vm.Y = "9"; vm.Z = "9";
        vm.TeleportCommand.Execute(null);

        var alice = ws.Current!.Players.Single(p => p.Name == "Alice");
        var bob = ws.Current!.Players.Single(p => p.Name == "Bob");
        Assert.Equal(("Prime", "9,9,9"), (bob.PlanetId, bob.PlayerPosition));
        Assert.Equal(("Prime", "1,1,1"), (alice.PlanetId, alice.PlayerPosition)); // untouched

        // Back to Alice — form shows Alice's still-unchanged state.
        vm.SelectedPlayer = vm.Players.Single(p => p.Name == "Alice");
        Assert.Equal("Prime", vm.SelectedPlanetId);
        Assert.Equal(("1", "1", "1"), (vm.X, vm.Y, vm.Z));
    }
}
