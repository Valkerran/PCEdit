using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PCEdit.App.Core.Services;

/// <summary>
/// <see cref="ISaveBackupService"/> that copies the save into a directory the UI head chooses,
/// before PCEdit first writes over it.
/// </summary>
public sealed class FileSaveBackupService(string backupDirectory, Func<DateTime>? utcNow = null) : ISaveBackupService
{
    /// <summary>How many copies of any one save file to keep.</summary>
    private const int KeepPerSaveFile = 5;

    private const string BackupExtension = ".bak";

    private readonly Func<DateTime> _utcNow = utcNow ?? (() => DateTime.UtcNow);

    public string DirectoryPath { get; } = Path.GetFullPath(backupDirectory);

    public void BackUp(string savePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

        // "Save As" onto a path with nothing on it yet has nothing to preserve.
        if (!File.Exists(savePath))
        {
            return;
        }

        Directory.CreateDirectory(DirectoryPath);

        // No ':' in the stamp - illegal in a Windows filename. This shape sorts lexicographically
        // in chronological order, which is what lets Prune pick the newest without reading file
        // timestamps. Milliseconds, and never overwriting, so two backups in one second are both
        // kept (issue #101).
        var stamp = _utcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var prefix = PrefixFor(savePath);

        var backup = Path.Combine(DirectoryPath, prefix + stamp + BackupExtension);
        File.Copy(savePath, backup, overwrite: false);

        // The copy carries a read-only flag with it, and Windows will not delete a read-only
        // file - Prune would leave that backup behind for good.
        File.SetAttributes(backup, FileAttributes.Normal);

        Prune(prefix);
    }

    /// <summary>
    /// "name.hash." - the hash is of the save's full path, so two saves with the same file name
    /// in different folders (Steam and Game Pass both have Survival-1.json) do not share one
    /// prune budget (issue #101).
    /// </summary>
    private static string PrefixFor(string savePath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(savePath)));
        return $"{Path.GetFileName(savePath)}.{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}.";
    }

    /// <summary>Deletes all but the newest <see cref="KeepPerSaveFile"/> copies of one save.
    /// Backups named by an older version carry no hash, match no prefix, and are left alone.</summary>
    private void Prune(string prefix)
    {
        // Filtered in code rather than with a search pattern: wildcard matching on Windows also
        // matches legacy 8.3 names, which would sweep in files this did not create. Ordinal, so
        // two saves differing only in case stay distinct on a case-sensitive filesystem.
        var stale = Directory.EnumerateFiles(DirectoryPath, "*" + BackupExtension)
            .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(KeepPerSaveFile);

        foreach (var path in stale)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // A backup that cannot be pruned is not worth failing the save over.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
