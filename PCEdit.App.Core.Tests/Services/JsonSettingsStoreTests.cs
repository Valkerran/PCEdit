using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"pcedit-settings-{Guid.NewGuid():N}");

    private string SettingsPath => Path.Combine(_directory, "PCEdit", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void ASavedSetting_IsReadBackByANewStore()
    {
        new JsonSettingsStore(SettingsPath).SaveCulture("fr");

        Assert.Equal("fr", new JsonSettingsStore(SettingsPath).GetSavedCulture());
    }

    [Fact]
    public void TwoStores_ChangingDifferentSettings_BothSurvive()
    {
        // Two PCEdit windows: each used to rewrite the whole file from the snapshot it took at
        // startup, wiping whatever the other had saved since (issue #100).
        var first = new JsonSettingsStore(SettingsPath);
        var second = new JsonSettingsStore(SettingsPath);

        first.SaveCulture("de");
        second.SaveStackingMode(StackingMode.Always);

        var reopened = new JsonSettingsStore(SettingsPath);
        Assert.Equal("de", reopened.GetSavedCulture());
        Assert.Equal(StackingMode.Always, reopened.GetStackingMode());
    }

    [Fact]
    public void AWrite_LeavesOnlyTheSettingsFile()
    {
        new JsonSettingsStore(SettingsPath).Acknowledge();

        Assert.Equal([SettingsPath], Directory.GetFiles(Path.GetDirectoryName(SettingsPath)!));
    }

    [Fact]
    public void AnUnreadableFile_FallsBackToDefaults_AndTheNextWriteReplacesIt()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");

        var store = new JsonSettingsStore(SettingsPath);
        Assert.Null(store.GetSavedCulture());
        Assert.False(store.HasAcknowledged);

        store.SaveCulture("it");
        Assert.Equal("it", new JsonSettingsStore(SettingsPath).GetSavedCulture());
    }
}
