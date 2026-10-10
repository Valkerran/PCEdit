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
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = new TeleportViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(workspace));
        vm.Load();
        return vm;
    }

    [Fact]
    public void Load_WithAnUnreadablePlayerPosition_DoesNotBringDownThePage()
    {
        // Load selects the first player, which read its position through the throwing parse - so
        // a corrupt position killed the app on navigating to Teleport, after the file had already
        // opened successfully (issue #37).
        var save = WorkspaceFixtures.Create();
        save.Players[0] = save.Players[0] with { PlayerPosition = "not,a,position" };
        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = new TeleportViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(workspace));

        vm.Load();

        Assert.NotEmpty(vm.Players);
    }

    [Fact]
    public void Landmarks_WithAnUnreadablePosition_AreLeftOutOfTheList()
    {
        var save = WorkspaceFixtures.Create();
        save.WorldObjects.Add(new WorldObject { Id = 600, GId = "EscapePod", Position = "broken", Planet = PlanetHash.Of("Prime") });
        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = new TeleportViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(workspace));

        vm.Load();

        // Useless as a teleport target, so it never reaches the user rather than failing on click.
        Assert.DoesNotContain(vm.Landmarks, l => l.WorldObjectId == 600);
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
        var (vm, ws) = CreateMultiplayer();

        // Form starts on the first player.
        Assert.Equal("Alice", vm.SelectedPlayer!.Name);
        AssertFormShows(vm, "Prime", "1");

        // Switch to Bob — the whole form re-initialises from Bob.
        vm.SelectedPlayer = vm.Players.Single(p => p.Name == "Bob");
        AssertFormShows(vm, "Aqualis", "2");

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
        AssertFormShows(vm, "Prime", "1");

        Assert.True(vm.HasMultiplePlayers);
    }

    /// <summary>The form's world, and the same value in each of X, Y and Z.</summary>
    private static void AssertFormShows(TeleportViewModel vm, string planetId, string coordinate)
    {
        Assert.Equal(planetId, vm.SelectedPlanetId);
        Assert.Equal((coordinate, coordinate, coordinate), (vm.X, vm.Y, vm.Z));
    }

    /// <summary>Alice (id 1) on Prime at 1,1,1 and Bob (id 2) on Aqualis at 2,2,2.</summary>
    private static (TeleportViewModel Vm, SaveFileWorkspace Workspace) CreateMultiplayer()
    {
        var save = WorkspaceFixtures.CreateMultiWorld();
        var ai = save.Players.FindIndex(p => p.Name == "Alice");
        var bi = save.Players.FindIndex(p => p.Name == "Bob");
        save.Players[ai] = save.Players[ai] with { PlayerPosition = "1,1,1" };
        save.Players[bi] = save.Players[bi] with { PlayerPosition = "2,2,2" };

        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var ws = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        ws.Load(Path);
        var vm = new TeleportViewModel(ws, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(ws));
        vm.Load();
        return (vm, ws);
    }

    [Fact]
    public void HasMultiplePlayers_IsFalseOnASoloSave()
    {
        var save = WorkspaceFixtures.CreateMultiWorld();
        save.Players.RemoveAll(p => p.Name != "Alice");
        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var ws = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        ws.Load(Path);
        var vm = new TeleportViewModel(ws, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(ws));
        vm.Load();

        Assert.False(vm.HasMultiplePlayers);
    }

    private static (TeleportViewModel Vm, SaveFileWorkspace Workspace, Localizer Localizer) CreateWithWorkspace(
        Action<PlanetCrafterSaveFile>? arrange = null)
    {
        var save = WorkspaceFixtures.Create(); // Alice (id 1) is on "Prime" at 0,0,0
        arrange?.Invoke(save);
        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = new TeleportViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService(), new PlanetIndex(workspace));
        vm.Load();
        return (vm, workspace, localizer);
    }

    // "-120,5" used to teleport the player to -1205 with a success message (issue #72).
    [Theory]
    [InlineData("-120,5")]
    [InlineData("12,5")]
    [InlineData("1,234.5")]
    [InlineData("5-")]
    [InlineData("")]
    [InlineData("abc")]
    public void Teleport_CoordinateThatIsNotAPlainNumber_IsRefused_AndThePlayerStaysPut(string x)
    {
        var (vm, workspace, localizer) = CreateWithWorkspace();
        vm.X = x;
        vm.Y = "1";
        vm.Z = "1";

        vm.TeleportCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal(localizer[LocKeys.Teleport_InvalidCoords], vm.StatusMessage);
        Assert.Equal("0,0,0", workspace.Current!.Players.Single(p => p.Id == 1).PlayerPosition);
        Assert.False(workspace.IsDirty);
    }

    [Theory]
    [InlineData("-120.5", "-120.5")]
    [InlineData(" 12.5 ", "12.5")]
    [InlineData("1.5e2", "150")]
    public void Teleport_PlainNumber_IsWritten(string x, string writtenX)
    {
        var (vm, workspace, _) = CreateWithWorkspace();
        vm.X = x;
        vm.Y = "1";
        vm.Z = "1";

        vm.TeleportCommand.Execute(null);

        Assert.Equal($"{writtenX},1,1", workspace.Current!.Players.Single(p => p.Id == 1).PlayerPosition);
    }

    // " Aqualis " used to be written as typed and matched no planet (issue #97).
    [Fact]
    public void Teleport_CustomPlanetId_IsTrimmed()
    {
        var (vm, workspace, localizer) = CreateWithWorkspace();
        vm.SelectedPlanetId = localizer[LocKeys.Teleport_OtherPlanet];
        vm.CustomPlanetId = " Aqualis ";
        vm.X = vm.Y = vm.Z = "1";

        vm.TeleportCommand.Execute(null);

        Assert.Equal("Aqualis", workspace.Current!.Players.Single(p => p.Id == 1).PlanetId);
    }

    // Load() assigned a player option equal to the one already selected, so the change handler
    // never ran and the form kept whatever was in it (issue #93).
    [Fact]
    public void Load_Again_ResetsTheFormToThePlayer()
    {
        var (vm, _, _) = CreateWithWorkspace();
        vm.X = "999";
        vm.SelectedPlanetId = "Somewhere";

        vm.Load();

        Assert.Equal("0", vm.X);
        Assert.Equal("Prime", vm.SelectedPlanetId);
    }

    // IsCustomPlanetId compared the selection with the *current* translation of "Other…", so a
    // language change turned a custom-planet selection into a literal planet id (issue #93).
    [Fact]
    public void IsCustomPlanetId_DoesNotDependOnTheCurrentLanguage()
    {
        var (vm, _, localizer) = CreateWithWorkspace();
        vm.SelectedPlanetId = localizer[LocKeys.Teleport_OtherPlanet];

        localizer.SetCulture("de");

        Assert.True(vm.IsCustomPlanetId);
    }

    // The boxes used to keep whatever they held - another player's or another world's
    // coordinates - and "Use current position" still reported success (issue #92).
    [Fact]
    public void Load_PlayerWithAnUnreadablePosition_EmptiesTheCoordinates_AndSaysWhy()
    {
        var (vm, _, localizer) = CreateWithWorkspace(save =>
            save.Players[0] = save.Players[0] with { PlayerPosition = "not,a,position" });

        Assert.Equal(string.Empty, vm.X);
        Assert.Equal(string.Empty, vm.Y);
        Assert.Equal(string.Empty, vm.Z);
        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal(localizer.Format(LocKeys.Teleport_PositionUnreadable, "Alice"), vm.StatusMessage);
    }

    [Fact]
    public void UseCurrentPosition_WithAnUnreadablePosition_ReportsAnError_AndCannotBeTeleportedTo()
    {
        var (vm, workspace, localizer) = CreateWithWorkspace(save =>
            save.Players[0] = save.Players[0] with { PlayerPosition = "not,a,position" });
        vm.X = vm.Y = vm.Z = "42";

        vm.UseCurrentPositionCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal(localizer.Format(LocKeys.Teleport_PositionUnreadable, "Alice"), vm.StatusMessage);
        Assert.Equal(string.Empty, vm.X);

        vm.TeleportCommand.Execute(null);

        Assert.Equal("not,a,position", workspace.Current!.Players.Single(p => p.Id == 1).PlayerPosition);
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void SelectingAnotherPlayer_ClearsThePreviousStatus()
    {
        var (vm, _, _) = CreateWithWorkspace(save =>
            save.Players[0] = save.Players[0] with { PlayerPosition = "not,a,position" });
        Assert.Equal(StatusKind.Error, vm.StatusKind);

        vm.SelectedPlayer = vm.Players[1]; // Bob's position is readable

        Assert.Null(vm.StatusMessage);
        Assert.Equal("0", vm.X);
    }

    // A language change used to reload the page, discarding typed coordinates (issue #94).
    [Fact]
    public void Relocalize_KeepsTypedInput_AndTranslatesTheOtherEntry()
    {
        var (vm, _, localizer) = CreateWithWorkspace();
        vm.X = "123.5";
        vm.CustomPlanetId = "Typed";

        localizer.SetCulture("de");
        vm.Relocalize();

        Assert.Equal("123.5", vm.X);
        Assert.Equal("Typed", vm.CustomPlanetId);
        Assert.Equal(localizer[LocKeys.Teleport_OtherPlanet], vm.PlanetIdOptions.Last());
        Assert.Equal("Prime", vm.SelectedPlanetId);
    }

    [Fact]
    public void Relocalize_WithACustomPlanetSelected_KeepsItSelected_AndTeleportsToTheTypedId()
    {
        var (vm, workspace, localizer) = CreateWithWorkspace();
        vm.SelectedPlanetId = localizer[LocKeys.Teleport_OtherPlanet];
        vm.CustomPlanetId = "Aqualis";
        vm.X = vm.Y = vm.Z = "1";

        localizer.SetCulture("de");
        vm.Relocalize();

        Assert.True(vm.IsCustomPlanetId);
        Assert.Equal(localizer[LocKeys.Teleport_OtherPlanet], vm.SelectedPlanetId);

        vm.TeleportCommand.Execute(null);

        Assert.Equal("Aqualis", workspace.Current!.Players.Single(p => p.Id == 1).PlanetId);
    }

    [Fact]
    public void Relocalize_ClearsAStatusWrittenInTheOldLanguage()
    {
        var (vm, _, localizer) = CreateWithWorkspace();
        vm.X = "1,5";
        vm.TeleportCommand.Execute(null);
        Assert.NotNull(vm.StatusMessage);

        localizer.SetCulture("de");
        vm.Relocalize();

        Assert.Null(vm.StatusMessage);
    }

    private static void AddAqualisWithAnArrivalPod(PlanetCrafterSaveFile save)
    {
        save.Terraformations.Add(new PlanetTerraformation { PlanetId = "Aqualis" });
        save.WorldObjects.Add(new WorldObject
        {
            Id = 503, GId = "EscapePodInterplanetary", Position = "42,0,42", Planet = PlanetHash.Of("Aqualis")
        });
    }

    // Picking another world fills in its arrival point. Coming back to the player's own world
    // must not leave those coordinates in the boxes when the player's position cannot be read:
    // Teleport would write the other world's coordinates onto this one (issue #92).
    [Fact]
    public void ReturningToThePlayersWorld_WithAnUnreadablePosition_EmptiesTheCoordinates_AndSaysWhy()
    {
        var (vm, _, localizer) = CreateWithWorkspace(save =>
        {
            AddAqualisWithAnArrivalPod(save);
            save.Players[0] = save.Players[0] with { PlayerPosition = "not,a,position" };
        });
        vm.SelectedPlanetId = "Aqualis";
        Assert.Equal("42", vm.X);

        vm.SelectedPlanetId = "Prime";

        Assert.Equal((string.Empty, string.Empty, string.Empty), (vm.X, vm.Y, vm.Z));
        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal(localizer.Format(LocKeys.Teleport_PositionUnreadable, "Alice"), vm.StatusMessage);
    }

    // "Aimed at Aqualis" used to stay on screen after the destination went back to the player's
    // own world and the coordinates with it.
    [Fact]
    public void ChangingTheDestination_ClearsTheStatusAboutThePreviousOne()
    {
        var (vm, _, _) = CreateWithWorkspace(AddAqualisWithAnArrivalPod);
        vm.SelectedPlanetId = "Aqualis";
        Assert.NotNull(vm.StatusMessage);

        vm.SelectedPlanetId = "Prime";

        Assert.Null(vm.StatusMessage);
        Assert.Equal("0", vm.X);
    }

    // What the real dropdown does when its selected entry is replaced: it clears the selection
    // (writing null back) before the view model assigns the translated entry. A headless run of
    // the real view showed this order; the test reproduces it, since no view is loaded here.
    [Fact]
    public void Relocalize_WhenTheDropdownClearsItsSelectionFirst_StillKeepsTheCustomPlanetAndCoordinates()
    {
        var (vm, workspace, localizer) = CreateWithWorkspace();
        vm.SelectedPlanetId = localizer[LocKeys.Teleport_OtherPlanet];
        vm.CustomPlanetId = "Aqualis";
        vm.X = vm.Y = vm.Z = "7";
        vm.PlanetIdOptions.CollectionChanged += (_, change) =>
        {
            if (change.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                vm.SelectedPlanetId = null;
            }
        };

        localizer.SetCulture("de");
        vm.Relocalize();

        Assert.True(vm.IsCustomPlanetId);
        Assert.Equal(("7", "7", "7"), (vm.X, vm.Y, vm.Z));
        vm.TeleportCommand.Execute(null);
        Assert.Equal("Aqualis", workspace.Current!.Players.Single(p => p.Id == 1).PlanetId);
    }
}
