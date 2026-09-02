using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;

namespace PCEdit.App.Core.Tests.Services;

public sealed class SaveFileWorkspaceTests
{
    private const string Path = @"C:\fake\save.txt";

    private static (SaveFileWorkspace Workspace, FakeSaveFileStore Store) CreateLoadedWorkspace()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), new Localizer(), new FakeSaveBackupService());
        workspace.Load(Path);
        return (workspace, store);
    }

    private static (SaveFileWorkspace Workspace, FakeSaveFileStore Store, FakeSaveBackupService Backups)
        CreateLoadedWorkspaceWithBackups()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var backups = new FakeSaveBackupService();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), new Localizer(), backups);
        workspace.Load(Path);
        workspace.GrantTerraTokens(1, 1);
        return (workspace, store, backups);
    }

    [Fact]
    public void Save_BacksUpTheFileBeforeWritingOverIt()
    {
        var (workspace, store, backups) = CreateLoadedWorkspaceWithBackups();
        var saveCountWhenBackedUp = -1;
        backups.OnBackUp = () => saveCountWhenBackedUp = store.SaveCallCount;

        workspace.Save();

        Assert.Equal(Path, Assert.Single(backups.BackedUpPaths));

        // Backing up after the write would copy PCEdit's own output; the pristine file is the
        // only copy that cannot be reconstructed, so the order here is the whole point.
        Assert.Equal(0, saveCountWhenBackedUp);
    }

    [Fact]
    public void Save_Twice_BacksUpOnlyTheFirstTime()
    {
        var (workspace, _, backups) = CreateLoadedWorkspaceWithBackups();

        workspace.Save();
        workspace.GrantTerraTokens(1, 1);
        workspace.Save();

        // By the second save the file on disk is already PCEdit's output, not the original.
        Assert.Single(backups.BackedUpPaths);
    }

    [Fact]
    public void Save_AfterLoadingAgain_BacksUpTheNewlyLoadedFile()
    {
        var (workspace, _, backups) = CreateLoadedWorkspaceWithBackups();
        workspace.Save();

        workspace.Load(Path);
        workspace.GrantTerraTokens(1, 1);
        workspace.Save();

        Assert.Equal(2, backups.BackedUpPaths.Count);
    }

    [Fact]
    public void Save_WhenTheBackupFails_StillWritesTheSave()
    {
        // Refusing to write the player's edit because a safety copy could not be taken inverts
        // the priority - the edit is the thing they actually asked for.
        var (workspace, store, backups) = CreateLoadedWorkspaceWithBackups();
        backups.ThrowOnBackUp = new IOException("backup directory is unwritable");

        workspace.Save();

        Assert.Equal(1, store.SaveCallCount);
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void Load_PopulatesCurrentAndFilePath_AndClearsDirty()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.True(workspace.IsLoaded);
        Assert.Equal(Path, workspace.FilePath);
        Assert.False(workspace.IsDirty);
        Assert.NotNull(workspace.Current);
    }

    [Fact]
    public void Save_BeforeLoad_ThrowsInvalidOperationException()
    {
        var workspace = new SaveFileWorkspace(new FakeSaveFileStore(), new FakeScreenReaderAnnouncer(), new Localizer(), new FakeSaveBackupService());

        Assert.Throws<InvalidOperationException>(() => workspace.Save());
    }

    [Fact]
    public void Save_WritesThroughStore_AndClearsDirty()
    {
        var (workspace, store) = CreateLoadedWorkspace();
        workspace.ReplaceTerraformation("Prime", t => new PCEdit.SaveFileHandler.Models.PlanetTerraformation
        {
            PlanetId = t.PlanetId,
            UnitOxygenLevel = 99m,
            UnitHeatLevel = t.UnitHeatLevel,
            UnitPressureLevel = t.UnitPressureLevel,
            UnitPlantsLevel = t.UnitPlantsLevel,
            UnitInsectsLevel = t.UnitInsectsLevel,
            UnitAnimalsLevel = t.UnitAnimalsLevel,
            UnitPurificationLevel = t.UnitPurificationLevel
        });
        Assert.True(workspace.IsDirty);

        workspace.Save();

        Assert.False(workspace.IsDirty);
        Assert.Equal(1, store.SaveCallCount);
    }

    [Fact]
    public void MutateUnlocks_RebuildsRoot_PreservingOtherSections_AndSetsDirty()
    {
        var (workspace, _) = CreateLoadedWorkspace();
        var before = workspace.Current!;
        var beforePlayers = before.Players;
        var beforeWorldObjects = before.WorldObjects;

        workspace.MutateUnlocks(u => new PCEdit.SaveFileHandler.Models.SaveFileUnlocks
        {
            TerraTokens = 999,
            AllTimeTerraTokens = u.AllTimeTerraTokens,
            UnlockedGroups = u.UnlockedGroups,
            OpenedInstanceSeed = u.OpenedInstanceSeed,
            OpenedInstanceTimeLeft = u.OpenedInstanceTimeLeft
        });

        var after = workspace.Current!;
        Assert.NotSame(before, after);
        Assert.Equal(999, after.Unlocks.TerraTokens);
        // Root was rebuilt but the untouched list instances are carried over by reference.
        Assert.Same(beforePlayers, after.Players);
        Assert.Same(beforeWorldObjects, after.WorldObjects);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ReplaceTerraformation_ReplacesElementInPlace_WithoutRebuildingRoot()
    {
        var (workspace, _) = CreateLoadedWorkspace();
        var before = workspace.Current!;
        var terraformationsList = before.Terraformations;

        workspace.ReplaceTerraformation("Prime", t => new PCEdit.SaveFileHandler.Models.PlanetTerraformation
        {
            PlanetId = t.PlanetId,
            UnitOxygenLevel = 500m,
            UnitHeatLevel = t.UnitHeatLevel,
            UnitPressureLevel = t.UnitPressureLevel,
            UnitPlantsLevel = t.UnitPlantsLevel,
            UnitInsectsLevel = t.UnitInsectsLevel,
            UnitAnimalsLevel = t.UnitAnimalsLevel,
            UnitPurificationLevel = t.UnitPurificationLevel
        });

        Assert.Same(before, workspace.Current);
        Assert.Same(terraformationsList, workspace.Current!.Terraformations);
        Assert.Equal(500m, workspace.Current.Terraformations.Single(t => t.PlanetId == "Prime").UnitOxygenLevel);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ReplaceTerraformation_UnknownPlanetId_ThrowsInvalidOperationException()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Throws<InvalidOperationException>(() => workspace.ReplaceTerraformation("NoSuchPlanet", t => t));
    }

    [Fact]
    public void ReplacePlayer_ReplacesElementInPlace_WithoutRebuildingRoot()
    {
        var (workspace, _) = CreateLoadedWorkspace();
        var before = workspace.Current!;
        var playersList = before.Players;

        workspace.ReplacePlayer(1, p => new PCEdit.SaveFileHandler.Models.PlayerData
        {
            Id = p.Id,
            Name = "Alice Renamed",
            InventoryId = p.InventoryId,
            EquipmentId = p.EquipmentId,
            PlayerPosition = p.PlayerPosition,
            PlayerRotation = p.PlayerRotation,
            PlayerGaugeOxygen = p.PlayerGaugeOxygen,
            PlayerGaugeThirst = p.PlayerGaugeThirst,
            PlayerGaugeHealth = p.PlayerGaugeHealth,
            PlayerGaugeToxic = p.PlayerGaugeToxic,
            Host = p.Host,
            PlanetId = p.PlanetId,
            TotalCraftedObjects = p.TotalCraftedObjects,
            TotalTerraTokenEarned = p.TotalTerraTokenEarned,
            CameraView = p.CameraView
        });

        // Root instance is unchanged; only the list element and IsDirty change.
        Assert.Same(before, workspace.Current);
        Assert.Same(playersList, workspace.Current!.Players);
        Assert.Equal("Alice Renamed", workspace.Current.Players.Single(p => p.Id == 1).Name);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ReplacePlayer_UnknownId_ThrowsInvalidOperationException()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Throws<InvalidOperationException>(() => workspace.ReplacePlayer(12345, p => p));
    }

    [Fact]
    public void ReplaceInventory_ReplacesElementInPlace_WithoutRebuildingRoot()
    {
        var (workspace, _) = CreateLoadedWorkspace();
        var before = workspace.Current!;
        var inventoriesList = before.Inventories;

        workspace.ReplaceInventory(10, inv => new PCEdit.SaveFileHandler.Models.Inventory
        {
            Id = inv.Id,
            WorldObjectIds = "200",
            Size = inv.Size,
            DemandGroups = inv.DemandGroups,
            SupplyGroups = inv.SupplyGroups,
            Priority = inv.Priority
        });

        Assert.Same(before, workspace.Current);
        Assert.Same(inventoriesList, workspace.Current!.Inventories);
        Assert.Equal("200", workspace.Current.Inventories.Single(i => i.Id == 10).WorldObjectIds);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void ReplaceInventory_UnknownId_ThrowsInvalidOperationException()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Throws<InvalidOperationException>(() => workspace.ReplaceInventory(12345, i => i));
    }

    [Fact]
    public void GrantTerraTokens_NonPositiveAmount_ThrowsArgumentOutOfRangeException()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Throws<ArgumentOutOfRangeException>(() => workspace.GrantTerraTokens(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => workspace.GrantTerraTokens(1, -5));
    }

    [Fact]
    public void GrantTerraTokens_IncreasesUnlocksAndPlayerTotals()
    {
        var (workspace, _) = CreateLoadedWorkspace();
        var unlocksBefore = workspace.Current!.Unlocks;
        var playerBefore = workspace.Current.Players.Single(p => p.Id == 1);

        workspace.GrantTerraTokens(1, 50);

        var unlocksAfter = workspace.Current!.Unlocks;
        var playerAfter = workspace.Current.Players.Single(p => p.Id == 1);
        Assert.Equal(unlocksBefore.TerraTokens + 50, unlocksAfter.TerraTokens);
        Assert.Equal(unlocksBefore.AllTimeTerraTokens + 50, unlocksAfter.AllTimeTerraTokens);
        Assert.Equal(playerBefore.TotalTerraTokenEarned + 50, playerAfter.TotalTerraTokenEarned);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void GrantTerraTokens_PastTheIntCeiling_ClampsInsteadOfWrappingNegative()
    {
        // Unchecked int addition wrapped a large grant to a negative balance and wrote it into
        // the save: 8,680 + int.MaxValue came out as -2,147,474,969 (issue #42).
        var (workspace, _) = CreateLoadedWorkspace();

        workspace.GrantTerraTokens(1, int.MaxValue);

        var save = workspace.Current!;
        Assert.Equal(int.MaxValue, save.Unlocks.TerraTokens);
        Assert.Equal(int.MaxValue, save.Unlocks.AllTimeTerraTokens);
        Assert.Equal(int.MaxValue, save.Players.Single(p => p.Id == 1).TotalTerraTokenEarned);
    }

    [Fact]
    public void GrantTerraTokens_ReturnsWhatTheBalanceCouldActuallyTake()
    {
        // The fixture starts at 100 tokens, so a max grant is short by exactly that. The status
        // line reports this figure, which is why it has to be the real one.
        var (workspace, _) = CreateLoadedWorkspace();

        var granted = workspace.GrantTerraTokens(1, int.MaxValue);

        Assert.Equal(int.MaxValue - 100, granted);
    }

    [Fact]
    public void GrantTerraTokens_WithinRange_ReturnsTheWholeAmount()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Equal(50, workspace.GrantTerraTokens(1, 50));
    }

    [Fact]
    public void GrantTerraTokens_UnknownPlayer_ThrowsInvalidOperationException()
    {
        var (workspace, _) = CreateLoadedWorkspace();

        Assert.Throws<InvalidOperationException>(() => workspace.GrantTerraTokens(12345, 10));
    }
}
