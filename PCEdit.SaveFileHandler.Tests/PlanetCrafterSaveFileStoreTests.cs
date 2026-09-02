using PCEdit.SaveFileHandler.Tests.Fixtures;

namespace PCEdit.SaveFileHandler.Tests;

public sealed class PlanetCrafterSaveFileStoreTests : IDisposable
{
    private readonly PlanetCrafterSaveFileStore _store = new(new PlanetCrafterSaveFileSerializer(new JsonRecordSerializer()));
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"pcedit-test-{Guid.NewGuid():N}.txt");

    /// <summary>Where Save stages its write before swapping it in.</summary>
    private string TempPath => _tempFile + ".pcedit-tmp";

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        // A completed save leaves nothing here; one test occupies the path on purpose.
        if (Directory.Exists(TempPath))
        {
            Directory.Delete(TempPath, recursive: true);
        }
        else if (File.Exists(TempPath))
        {
            File.Delete(TempPath);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_NullOrWhitespacePath_ThrowsArgumentException(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => _store.Load(path!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_NullOrWhitespacePath_ThrowsArgumentException(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => _store.Save(path!, SaveFileFixtures.CreateEmpty()));
    }

    [Fact]
    public void Save_NullSaveFile_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _store.Save(_tempFile, null!));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsThroughDisk()
    {
        var original = SaveFileFixtures.CreateFullyPopulated();

        _store.Save(_tempFile, original);
        var loaded = _store.Load(_tempFile);

        Assert.Equal(original.Unlocks.UnlockedGroups, loaded.Unlocks.UnlockedGroups);
        Assert.Equal(original.Players[0].Name, loaded.Players[0].Name);
        Assert.Equal(original.WorldObjects[0].GId, loaded.WorldObjects[0].GId);
        Assert.Equal(original.WorldObjects[0].MineableCount, loaded.WorldObjects[0].MineableCount);
        Assert.Equal(original.WorldObjects[0].LinkedWorldObjectId, loaded.WorldObjects[0].LinkedWorldObjectId);
        Assert.Equal(original.WorldObjects[0].Text, loaded.WorldObjects[0].Text);
        Assert.Equal(original.Inventories[0].DemandGroups, loaded.Inventories[0].DemandGroups);
        Assert.Equal(original.Inventories[0].SupplyGroups, loaded.Inventories[0].SupplyGroups);
    }

    // BOM-carrying (Steam) fixtures only: this saves to a path that does not exist yet, which
    // is "Save As" and correctly emits a BOM. The BOM-less Game Pass fixture is covered by
    // Save_OverARealBomLessGamePassSave_KeepsItBomLess, which saves over the file itself.
    [Theory]
    [InlineData("Standard-2.json")]
    [InlineData("mini-save.json")]
    [InlineData("Humble-2.102.json")]
    public void SaveThenLoad_OfAnUnchangedSave_IsByteIdenticalOnDisk(string fixtureName)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", fixtureName);

        _store.Save(_tempFile, _store.Load(source));

        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(_tempFile));
    }

    [Fact]
    public void Save_ToANewPath_WritesUtf8WithBom()
    {
        // No file on disk yet ("Save As" / first save) — match the Steam game, which emits a BOM.
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());

        var bytes = File.ReadAllBytes(_tempFile);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
    }

    [Fact]
    public void Save_OverAFileThatHasNoBom_KeepsItBomLess()
    {
        // The Xbox / PC Game Pass (WGS) build stores the save with no BOM; adding one corrupts it.
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json");
        var bomLess = File.ReadAllBytes(source)[3..];
        File.WriteAllBytes(_tempFile, bomLess);

        _store.Save(_tempFile, _store.Load(_tempFile));

        var written = File.ReadAllBytes(_tempFile);
        Assert.NotEqual([0xEF, 0xBB, 0xBF], written[..3]);
        Assert.Equal(bomLess, written);
    }

    [Fact]
    public void Save_OverARealBomLessGamePassSave_KeepsItBomLess()
    {
        // Interplanetary-2.102.json is a raw Xbox / PC Game Pass (WGS) blob exactly as the game
        // wrote it -- no BOM. Adding one makes the game reject the save with "file error".
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "Interplanetary-2.102.json");
        File.Copy(source, _tempFile, overwrite: true);

        _store.Save(_tempFile, _store.Load(_tempFile));

        var written = File.ReadAllBytes(_tempFile);
        Assert.NotEqual<byte[]>([0xEF, 0xBB, 0xBF], written[..3]);
        Assert.Equal(File.ReadAllBytes(source), written);
    }
    [Fact]
    public void Save_OverAFileThatHasABom_KeepsTheBom()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json");
        var original = File.ReadAllBytes(source);
        File.WriteAllBytes(_tempFile, original);

        _store.Save(_tempFile, _store.Load(_tempFile));

        Assert.Equal(original, File.ReadAllBytes(_tempFile));
    }

    [Fact]
    public void Save_ToANewPath_LeavesNoTemporaryFileBehind()
    {
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());

        Assert.False(File.Exists(TempPath));
    }

    [Fact]
    public void Save_OverAnExistingFile_LeavesNoTemporaryFileBehind()
    {
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());

        _store.Save(_tempFile, _store.Load(_tempFile));

        Assert.False(File.Exists(TempPath));
    }

    [Fact]
    public void Save_WhenTheWriteFails_LeavesTheExistingSaveByteIdentical()
    {
        // Save used to be a File.WriteAllText straight over the target, which truncates it before
        // writing a byte - so anything that went wrong in between left the player with a ruined
        // save and no copy anywhere (issue #36).
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json");
        var original = File.ReadAllBytes(source);
        File.WriteAllBytes(_tempFile, original);

        // Occupy the staging path with a directory so the write cannot succeed. This stands in for
        // any interrupted write - full disk, permissions, power loss - which cannot be provoked
        // portably but reaches the target file the same way.
        Directory.CreateDirectory(TempPath);

        // The exact type differs by platform (UnauthorizedAccessException / IOException); that the
        // save reports failure at all is the point, and that the file below is untouched.
        Assert.ThrowsAny<Exception>(() => _store.Save(_tempFile, SaveFileFixtures.CreateEmpty()));

        Assert.Equal(original, File.ReadAllBytes(_tempFile));
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"pcedit-missing-{Guid.NewGuid():N}.txt");

        Assert.Throws<FileNotFoundException>(() => _store.Load(missingPath));
    }
}
