using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class FileSaveBackupServiceTests : IDisposable
{
    private readonly string _root = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"pcedit-backups-{Guid.NewGuid():N}")).FullName;

    private DateTime _now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    private string BackupDirectory => Path.Combine(_root, "backups");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private FileSaveBackupService Service() => new(BackupDirectory, () => _now);

    private string SaveIn(string folder, string content = "save")
    {
        var path = Path.Combine(Directory.CreateDirectory(Path.Combine(_root, folder)).FullName, "Survival-1.json");
        File.WriteAllText(path, content);
        return path;
    }

    private string[] Backups() => Directory.Exists(BackupDirectory) ? Directory.GetFiles(BackupDirectory) : [];

    [Fact]
    public void BackUp_CopiesTheSaveIntoTheBackupDirectory()
    {
        Service().BackUp(SaveIn("a", "pristine"));

        Assert.Equal("pristine", File.ReadAllText(Assert.Single(Backups())));
    }

    [Fact]
    public void BackUp_OfAPathWithNoFile_DoesNothing()
    {
        Service().BackUp(Path.Combine(_root, "missing.json"));

        Assert.Empty(Backups());
    }

    [Fact]
    public void SameNamedSavesInDifferentFolders_KeepSeparateBudgets()
    {
        // Keyed by file name alone, six backups of one Survival-1.json pruned the pristine copy
        // of another folder's Survival-1.json (issue #101).
        var service = Service();
        service.BackUp(SaveIn("steam", "steam pristine"));

        var gamePass = SaveIn("gamepass");
        for (var i = 0; i < 6; i++)
        {
            _now = _now.AddSeconds(1);
            service.BackUp(gamePass);
        }

        Assert.Equal(6, Backups().Length); // 1 + the newest 5 of the other
        Assert.Contains(Backups(), path => File.ReadAllText(path) == "steam pristine");
    }

    [Fact]
    public void TwoBackUpsWithinOneSecond_AreBothKept()
    {
        var service = Service();
        var save = SaveIn("a");

        service.BackUp(save);
        _now = _now.AddMilliseconds(1);
        service.BackUp(save);

        Assert.Equal(2, Backups().Length);
    }

    [Fact]
    public void BackupsInTheOldNameFormat_AreNeverPruned()
    {
        // They cannot be attributed to one folder, so deleting one risks someone's only
        // pristine copy.
        Directory.CreateDirectory(BackupDirectory);
        var old = Path.Combine(BackupDirectory, "Survival-1.json.20260101-000000.bak");
        File.WriteAllText(old, "old");

        var service = Service();
        var save = SaveIn("a");
        for (var i = 0; i < 6; i++)
        {
            _now = _now.AddSeconds(1);
            service.BackUp(save);
        }

        Assert.True(File.Exists(old));
        Assert.Equal(6, Backups().Length); // the old one + the newest 5
    }
}
