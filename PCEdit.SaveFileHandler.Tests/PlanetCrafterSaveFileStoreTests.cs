using System.Text;
using PCEdit.SaveFileHandler.Tests.Fixtures;

namespace PCEdit.SaveFileHandler.Tests;

public sealed class PlanetCrafterSaveFileStoreTests : IDisposable
{
    private readonly PlanetCrafterSaveFileStore _store = new(new PlanetCrafterSaveFileSerializer(new JsonRecordSerializer()));
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"pcedit-test-{Guid.NewGuid():N}.txt");

    private readonly string _copyFile = Path.Combine(Path.GetTempPath(), $"pcedit-test-{Guid.NewGuid():N}-copy.txt");

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        if (File.Exists(_copyFile))
        {
            File.Delete(_copyFile);
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
    [InlineData("Humble-2.103.json")]
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
    public void SaveCopy_OfABomLessGamePassSave_StaysBomLess_AndLeavesTheOriginalAlone()
    {
        // A repaired copy is a new path, which would normally get a BOM - and a BOM makes the
        // Game Pass build reject the save. The copy follows the original instead (issue #64).
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "Interplanetary-2.102.json");
        File.Copy(source, _tempFile, overwrite: true);

        _store.SaveCopy(_tempFile, _copyFile, _store.Load(_tempFile));

        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(_copyFile));
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(_tempFile));
    }

    [Fact]
    public void SaveCopy_OfASteamSave_KeepsTheBom()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json");
        File.Copy(source, _tempFile, overwrite: true);

        _store.SaveCopy(_tempFile, _copyFile, _store.Load(_tempFile));

        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(_copyFile));
    }

    [Fact]
    public void SaveCopy_OntoTheOriginalItself_IsRefused()
    {
        // The whole point of a copy is that the original survives; compared as full paths, so a
        // relative or differently-cased spelling of the same file is caught too.
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-save.json");
        File.Copy(source, _tempFile, overwrite: true);
        var sameFile = Path.Combine(Path.GetDirectoryName(_tempFile)!, ".", Path.GetFileName(_tempFile));

        Assert.Throws<ArgumentException>(() => _store.SaveCopy(_tempFile, sameFile, _store.Load(_tempFile)));
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(_tempFile));
    }

    [Fact]
    public void Save_ToANewPath_LeavesNoTemporaryFileBehind()
    {
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());

        Assert.Empty(LeftoverTempFiles());
    }

    [Fact]
    public void Save_OverAnExistingFile_LeavesNoTemporaryFileBehind()
    {
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());

        _store.Save(_tempFile, _store.Load(_tempFile));

        Assert.Empty(LeftoverTempFiles());
    }

    // Filtered in code: a "name.*" search pattern also matches "name" itself.
    private string[] LeftoverTempFiles() =>
        [.. Directory.EnumerateFiles(Path.GetTempPath())
            .Where(path => Path.GetFileName(path).StartsWith(Path.GetFileName(_tempFile) + ".", StringComparison.Ordinal))];

    [Fact]
    public void SaveCopy_OntoTheOriginalThroughASymlinkedDirectory_IsRefused()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // creating a symlink needs a privilege there
        }

        // Steam on Linux: ~/.steam/steam is a link to ~/.local/share/Steam. Opening through one
        // spelling and saving the copy through the other used to overwrite the original (#78).
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"pcedit-link-{Guid.NewGuid():N}")).FullName;
        try
        {
            var real = Directory.CreateDirectory(Path.Combine(root, "real")).FullName;
            var link = Path.Combine(root, "link");
            Directory.CreateSymbolicLink(link, real);
            var original = Path.Combine(real, "save.json");
            _store.Save(original, SaveFileFixtures.CreateEmpty());
            var before = File.ReadAllBytes(original);

            Assert.Throws<ArgumentException>(() =>
                _store.SaveCopy(original, Path.Combine(link, "save.json"), SaveFileFixtures.CreateFullyPopulated()));

            Assert.Equal(before, File.ReadAllBytes(original));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"pcedit-missing-{Guid.NewGuid():N}.txt");

        Assert.Throws<FileNotFoundException>(() => _store.Load(missingPath));
    }

    // A Latin-1 byte (0xE9) inside a player's name used to load as U+FFFD and be written back
    // over the original on the next save (issue #77). The byte goes inside a JSON string value,
    // so the framing still splits and only the strict decoder can reject it.
    [Fact]
    public void Load_InvalidUtf8_IsRejected()
    {
        var bytes = Encoding.UTF8.GetBytes(SerializedFixture());
        var marker = "\"name\":\""u8;
        var markerEnd = bytes.AsSpan().IndexOf(marker);
        Assert.True(markerEnd >= 0, "The fixture has no player name value to corrupt.");
        markerEnd += marker.Length;

        var corrupted = bytes.ToList();
        corrupted.Insert(markerEnd, 0xE9);
        File.WriteAllBytes(_tempFile, [.. corrupted]);

        var exception = Assert.Throws<InvalidDataException>(() => _store.Load(_tempFile));
        Assert.IsType<DecoderFallbackException>(exception.InnerException);
    }

    [Fact]
    public void Load_Utf16File_IsRejected()
    {
        File.WriteAllText(_tempFile, SerializedFixture(), Encoding.Unicode);

        Assert.Throws<InvalidDataException>(() => _store.Load(_tempFile));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Load_Utf8WithOrWithoutBom_Loads(bool withBom)
    {
        File.WriteAllText(_tempFile, SerializedFixture(), new UTF8Encoding(withBom));

        Assert.Single(_store.Load(_tempFile).WorldObjects);
    }

    [Fact]
    public void Fingerprint_OfAMissingFile_IsNull()
    {
        Assert.Null(_store.Fingerprint(_tempFile));
    }

    [Fact]
    public void Fingerprint_IsStableUntilTheFileChanges()
    {
        _store.Save(_tempFile, SaveFileFixtures.CreateEmpty());
        var before = _store.Fingerprint(_tempFile);

        Assert.Equal(before, _store.Fingerprint(_tempFile));

        File.AppendAllText(_tempFile, " "); // the game saving over it, in miniature
        Assert.NotEqual(before, _store.Fingerprint(_tempFile));
    }

    [Fact]
    public void Fingerprint_OfALinkedSave_ChangesWhenTheRealFileDoes()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // creating a symlink needs a privilege there
        }

        // Same length, so only the real file's write time can give the change away - the link
        // itself is never touched.
        File.WriteAllText(_tempFile, "one");
        File.SetLastWriteTimeUtc(_tempFile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.CreateSymbolicLink(_copyFile, _tempFile);
        var before = _store.Fingerprint(_copyFile);

        File.WriteAllText(_tempFile, "two");

        Assert.NotEqual(before, _store.Fingerprint(_copyFile));
    }

    private static string SerializedFixture() =>
        new PlanetCrafterSaveFileSerializer(new JsonRecordSerializer()).Serialize(SaveFileFixtures.CreateFullyPopulated());
}
