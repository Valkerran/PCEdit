namespace PCEdit.SaveFileHandler;

/// <summary>
/// A path with every symbolic link along it resolved, so two spellings of one file compare equal
/// and a write lands on the real file instead of replacing the link (issues #78, #87).
/// </summary>
/// <remarks>
/// Resolves links only. Windows 8.3 short names and Linux case-folding directories are not
/// canonicalized: both need platform calls, for a case nobody has reported.
/// </remarks>
internal static class RealPath
{
    // The kernel gives up at 40 nested links; a longer chain could not be opened anyway.
    private const int MaxNestedLinks = 40;

    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static string Of(string path) => Resolve(Path.GetFullPath(path), MaxNestedLinks);

    private static string Resolve(string fullPath, int linksLeft)
    {
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;
        var resolved = root;
        foreach (var part in fullPath[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, part);
            if (LinkTarget(resolved) is not { } target)
            {
                continue;
            }

            if (linksLeft == 0)
            {
                throw new IOException($"Too many levels of symbolic links in '{fullPath}'.");
            }

            // The target can itself sit under a linked directory, so it is resolved in turn.
            resolved = Resolve(target, linksLeft - 1);
        }

        return resolved;
    }

    /// <summary>Where a link points, one hop on, or null for anything that is not a link -
    /// including a path that does not exist yet.</summary>
    /// <remarks>
    /// One hop, not the final target: a relative target is relative to the folder the link
    /// really sits in, which only the walk above knows. Asking the runtime for the final target
    /// takes each hop from the link's spelled path instead, and a ".." after a linked directory
    /// then lands in the wrong folder.
    /// </remarks>
    private static string? LinkTarget(string path)
    {
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        return info.LinkTarget is { } target
            ? Path.GetFullPath(target, Path.GetDirectoryName(path) ?? path)
            : null;
    }
}
