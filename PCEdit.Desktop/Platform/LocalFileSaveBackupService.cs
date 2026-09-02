using System;
using System.Globalization;
using System.IO;
using System.Linq;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// <see cref="ISaveBackupService"/> that copies the save into
/// <c>&lt;LocalApplicationData&gt;/PCEdit/backups</c> before PCEdit first writes over it.
/// </summary>
public sealed class LocalFileSaveBackupService : ISaveBackupService
{
    /// <summary>How many copies of any one save file to keep.</summary>
    private const int KeepPerSaveFile = 5;

    private const string BackupExtension = ".bak";

    // LocalApplicationData, deliberately NOT the ApplicationData that settings.json uses: on
    // Windows that is the *roaming* profile, and several megabytes of save copies per edited file
    // has no business syncing between machines or counting against a profile quota. settings.json
    // is a few hundred bytes, so roaming it costs nothing and it stays where it is.
    //
    // Resolves to %LocalAppData% on Windows, ~/.local/share (or $XDG_DATA_HOME) on Linux, and
    // ~/Library/Application Support on macOS. That last one moved between .NET 7 and .NET 8, so
    // treat it as a deliberate choice rather than a fixed constant: a future runtime bump could
    // relocate it and make existing backups invisible.
    private static readonly string BackupDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        "PCEdit",
        "backups");

    public string DirectoryPath => BackupDirectory;

    public void BackUp(string savePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

        // "Save As" onto a path with nothing on it yet has nothing to preserve.
        if (!File.Exists(savePath))
        {
            return;
        }

        Directory.CreateDirectory(BackupDirectory);

        var sourceName = Path.GetFileName(savePath);

        // No ':' in the stamp - illegal in a Windows filename, and the path separator in classic
        // macOS. This shape also sorts lexicographically in chronological order, which is what
        // lets Prune pick the newest without reading file timestamps.
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        File.Copy(
            savePath,
            Path.Combine(BackupDirectory, sourceName + "." + stamp + BackupExtension),
            overwrite: true);

        Prune(sourceName);
    }

    /// <summary>Deletes all but the newest <see cref="KeepPerSaveFile"/> copies of one save.</summary>
    private static void Prune(string sourceName)
    {
        var prefix = sourceName + ".";

        // Filtered in code rather than with a search pattern: wildcard matching on Windows also
        // matches legacy 8.3 names, which would sweep in files this did not create. Ordinal, so
        // two saves differing only in case stay distinct on a case-sensitive filesystem.
        var stale = Directory.EnumerateFiles(BackupDirectory, "*" + BackupExtension)
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
