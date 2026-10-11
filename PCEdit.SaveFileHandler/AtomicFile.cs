namespace PCEdit.SaveFileHandler;

/// <summary>
/// Writes a whole file so that it is afterwards either wholly the old content or wholly the new,
/// never a half-written mixture. Used for save files and for PCEdit's own settings.
/// </summary>
public static class AtomicFile
{
    // The suffix keeps the transient file out of the *.json the game enumerates.
    private const string TempSuffix = ".pcedit-tmp";

    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromDays(1);

    /// <exception cref="UnauthorizedAccessException">The existing file is read-only.</exception>
    public static void WriteAllBytes(string path, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bytes);

        // A linked file is written where it really lives: the swap below replaces a directory
        // entry, so swapping onto the link would turn it into a plain file (issue #87).
        var target = RealPath.Of(path);
        ThrowIfReadOnly(target);

        // A sibling, because the swap is only atomic within one volume. Unique, so two writers
        // to one file cannot delete each other's staging file (issue #88).
        var temp = $"{target}.{Guid.NewGuid():N}{TempSuffix}";
        ClearAbandonedTempFiles(target);
        try
        {
            WriteThrough(temp, bytes);
            KeepPermissions(from: target, to: temp);

            // Atomic on every platform (MoveFileEx with REPLACE_EXISTING on Windows, rename on
            // Unix), and handles a destination that does not exist yet.
            File.Move(temp, target, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    /// <summary>
    /// A write that was interrupted leaves its staging file behind, and with a unique name per
    /// write nothing would ever reuse it. One this old cannot belong to a write in progress.
    /// </summary>
    private static void ClearAbandonedTempFiles(string target)
    {
        var directory = Path.GetDirectoryName(target);
        if (!Directory.Exists(directory))
        {
            return;
        }

        var prefix = Path.GetFileName(target) + ".";
        var abandonedBefore = DateTime.UtcNow - AbandonedAfter;

        try
        {
            // Filtered in code: wildcard matching on Windows also matches legacy 8.3 names.
            var abandoned = Directory.EnumerateFiles(directory, "*" + TempSuffix)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
                .Where(path => File.GetLastWriteTimeUtc(path) < abandonedBefore)
                .ToList();

            abandoned.ForEach(TryDelete);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Tidying up is not worth failing a save over.
        }
    }

    /// <summary>The swap needs only directory permission on Unix, so it would replace a file its
    /// owner made read-only on purpose. Windows refuses that by itself; this makes them agree.</summary>
    private static void ThrowIfReadOnly(string target)
    {
        if (File.Exists(target) && File.GetAttributes(target).HasFlag(FileAttributes.ReadOnly))
        {
            throw new UnauthorizedAccessException($"'{Path.GetFileName(target)}' is read-only.");
        }
    }

    /// <summary>
    /// Writes the content and forces it to the physical disk before returning. Without the flush
    /// the swap can complete while the new bytes are still only in the OS cache, so a power loss
    /// leaves the file present but empty - the very outcome the temp-and-swap exists to prevent.
    /// </summary>
    private static void WriteThrough(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>The new file replaces the old one, so it would otherwise swap the old one's
    /// permission bits for the process defaults.</summary>
    private static void KeepPermissions(string from, string to)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(from))
        {
            return;
        }

        var mode = File.GetUnixFileMode(from);
        if (mode == File.GetUnixFileMode(to))
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(to, mode);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // FAT, exFAT and some NTFS mounts (a Steam library on a shared drive) have no
            // per-file permissions to keep and refuse the call. That must not fail the save.
        }
    }

    /// <summary>Clears the temp file after a failed write. The original is intact by then, so a
    /// failure to clean up must not mask the error that actually mattered.</summary>
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
