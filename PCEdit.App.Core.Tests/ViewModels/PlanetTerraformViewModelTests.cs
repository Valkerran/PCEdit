using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;
using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class PlanetTerraformViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    // Applying an edit to one level must not reformat the others: a level the player left alone
    // keeps the exact text it was read with, so the save diff shows only the edit (issue #85).
    [Fact]
    public void Apply_LeavesUneditedLevelsAsTheyWereWritten()
    {
        var terraformation = new JsonRecordSerializer().Deserialize<PlanetTerraformation>(
            """{"planetId":"Prime","unitOxygenLevel":1.0,"unitHeatLevel":1234567890123456789.0,"unitPressureLevel":1E-05,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""",
            sectionIndex: 1);
        var (vm, workspace) = Create(terraformation);

        vm.OxygenLevel = "2";
        vm.ApplyCommand.Execute(null);

        var applied = Assert.Single(workspace.Current!.Terraformations);
        Assert.Equal("2.0", applied.UnitOxygenLevel.ToString());
        Assert.Equal("1234567890123456789.0", applied.UnitHeatLevel.ToString());
        Assert.Equal("1E-05", applied.UnitPressureLevel.ToString());
        Assert.Equal("-1.0", applied.UnitPurificationLevel.ToString());
    }

    [Fact]
    public void Load_ShowsAnExponentLevel_AndAppliesItBackUnchanged()
    {
        var terraformation = new JsonRecordSerializer().Deserialize<PlanetTerraformation>(
            """{"planetId":"Prime","unitOxygenLevel":1E+30,"unitHeatLevel":0.0,"unitPressureLevel":0.0,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""",
            sectionIndex: 1);
        var (vm, workspace) = Create(terraformation);

        vm.ApplyCommand.Execute(null);

        Assert.Equal("1E+30", vm.OxygenLevel);
        Assert.Equal("1E+30", Assert.Single(workspace.Current!.Terraformations).UnitOxygenLevel.ToString());
    }

    // A non-finite level would be written as "NaN.0" and make the save writer throw.
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e999")]
    public void Apply_RejectsANonFiniteLevel_AndChangesNothing(string text)
    {
        var terraformation = new PlanetTerraformation { PlanetId = "Prime", UnitOxygenLevel = 7.5 };
        var (vm, workspace) = Create(terraformation);

        vm.OxygenLevel = text;
        vm.ApplyCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal("7.5", Assert.Single(workspace.Current!.Terraformations).UnitOxygenLevel.ToString());
    }

    private static (PlanetTerraformViewModel Vm, SaveFileWorkspace Workspace) Create(PlanetTerraformation terraformation)
    {
        var save = WorkspaceFixtures.Create();
        save.Terraformations.Clear();
        save.Terraformations.Add(terraformation);
        var store = new FakeSaveFileStore();
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        return (new PlanetTerraformViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, terraformation), workspace);
    }

    private const string Levels =
        """{"planetId":"Prime","unitOxygenLevel":1.0,"unitHeatLevel":2.0,"unitPressureLevel":3.0,"unitPlantsLevel":4.0,"unitInsectsLevel":5.0,"unitAnimalsLevel":6.0,"unitPurificationLevel":7.0}""";

    private static PlanetTerraformation Read(string json) =>
        new JsonRecordSerializer().Deserialize<PlanetTerraformation>(json, sectionIndex: 1);

    // "1,5" used to be written as 15, "5-" as -5, and a negative level went straight in (issue #72).
    [Theory]
    [InlineData("1,5")]
    [InlineData("5-")]
    [InlineData("-1")]
    [InlineData("-0.5")]
    public void Apply_LevelThatIsNotAPlainNonNegativeNumber_IsRefused(string typed)
    {
        var (vm, workspace) = Create(Read(Levels));
        vm.OxygenLevel = typed;

        vm.ApplyCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal("1.0", Assert.Single(workspace.Current!.Terraformations).UnitOxygenLevel.ToString());
        Assert.False(workspace.IsDirty);
    }

    // -1 is how a save marks "no purification here"; typing it into a planet that has
    // purification used to hide the row for good on the next load (issue #72).
    [Fact]
    public void Apply_NegativePurification_IsRefused()
    {
        var (vm, workspace) = Create(Read(Levels));
        vm.PurificationLevel = "-1";

        vm.ApplyCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal("7.0", Assert.Single(workspace.Current!.Terraformations).UnitPurificationLevel.ToString());
    }

    // No real save is known to hold a negative level, but one that did must not lock the planet:
    // a negative value is accepted only when it is what the save already holds.
    [Fact]
    public void Apply_WithAnExistingNegativeLevelLeftAlone_StillAppliesTheOthers()
    {
        var (vm, workspace) = Create(Read(Levels.Replace("\"unitHeatLevel\":2.0", "\"unitHeatLevel\":-5.0")));
        vm.OxygenLevel = "9";

        vm.ApplyCommand.Execute(null);

        var applied = Assert.Single(workspace.Current!.Terraformations);
        Assert.Equal(StatusKind.Success, vm.StatusKind);
        Assert.Equal("9.0", applied.UnitOxygenLevel.ToString());
        Assert.Equal("-5.0", applied.UnitHeatLevel.ToString());
    }

    [Fact]
    public void Apply_ChangingAnExistingNegativeLevelToAnotherNegative_IsRefused()
    {
        var (vm, workspace) = Create(Read(Levels.Replace("\"unitHeatLevel\":2.0", "\"unitHeatLevel\":-5.0")));
        vm.HeatLevel = "-4";

        vm.ApplyCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal("-5.0", Assert.Single(workspace.Current!.Terraformations).UnitHeatLevel.ToString());
    }

    // "What the save already holds" means now, not when the card was built: once a negative
    // level has been replaced, typing the old negative value back in is a new negative level.
    [Fact]
    public void Apply_RetypingAnOldNegativeLevelAfterItWasReplaced_IsRefused()
    {
        var (vm, workspace) = Create(Read(Levels.Replace("\"unitHeatLevel\":2.0", "\"unitHeatLevel\":-5.0")));
        vm.HeatLevel = "3";
        vm.ApplyCommand.Execute(null);
        vm.HeatLevel = "-5";

        vm.ApplyCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal("3.0", Assert.Single(workspace.Current!.Terraformations).UnitHeatLevel.ToString());
    }
}
