using PCEdit.SaveFileHandler.Tests.Fixtures;

namespace PCEdit.SaveFileHandler.Tests;

public sealed class PlanetCrafterSaveFileStoreTests : IDisposable
{
    private readonly PlanetCrafterSaveFileStore _store = new(new PlanetCrafterSaveFileSerializer(new JsonRecordSerializer()));
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"pcedit-test-{Guid.NewGuid():N}.txt");

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
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

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"pcedit-missing-{Guid.NewGuid():N}.txt");

        Assert.Throws<FileNotFoundException>(() => _store.Load(missingPath));
    }
}
