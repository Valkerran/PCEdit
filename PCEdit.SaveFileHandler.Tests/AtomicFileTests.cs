namespace PCEdit.SaveFileHandler.Tests;

public sealed class AtomicFileTests : IDisposable
{
    private static readonly byte[] Original = [1, 2, 3];
    private static readonly byte[] Replacement = [9, 8, 7, 6];

    private readonly string _directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"pcedit-atomic-{Guid.NewGuid():N}")).FullName;

    private string Target => Path.Combine(_directory, "save.json");

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_directory))
        {
            if (new FileInfo(file).LinkTarget is null)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

        Directory.Delete(_directory, recursive: true);
    }

    private string[] FileNames() =>
        [.. Directory.EnumerateFileSystemEntries(_directory).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    [Fact]
    public void WriteAllBytes_ToANewPath_WritesTheBytes_AndLeavesNoTempFile()
    {
        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal(Replacement, File.ReadAllBytes(Target));
        Assert.Equal(["save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_OverAnExistingFile_ReplacesIt_AndLeavesNoTempFile()
    {
        File.WriteAllBytes(Target, Original);

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal(Replacement, File.ReadAllBytes(Target));
        Assert.Equal(["save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_OntoAReadOnlyFile_IsRefused_AndLeavesItUntouched()
    {
        // rename(2) needs only directory permission, so on Linux the swap used to replace a file
        // its owner had deliberately made read-only (issue #87).
        File.WriteAllBytes(Target, Original);
        File.SetAttributes(Target, FileAttributes.ReadOnly);

        Assert.Throws<UnauthorizedAccessException>(() => AtomicFile.WriteAllBytes(Target, Replacement));

        Assert.Equal(Original, File.ReadAllBytes(Target));
        Assert.Equal(["save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_WhenTheWriteFails_LeavesTheExistingFileByteIdentical_AndNoTempFile()
    {
        if (Environment.UserName == "root")
        {
            return; // root ignores directory permissions, so the failure cannot be provoked
        }

        File.WriteAllBytes(Target, Original);

        using (BlockWrites())
        {
            // The exact type differs by platform; that it fails at all is the point.
            Assert.ThrowsAny<Exception>(() => AtomicFile.WriteAllBytes(Target, Replacement));
        }

        Assert.Equal(Original, File.ReadAllBytes(Target));
        Assert.Equal(["save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_ThroughASymlink_WritesTheRealFile_AndKeepsTheLink()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // creating a symlink needs a privilege there
        }

        var real = Path.Combine(_directory, "real.json");
        File.WriteAllBytes(real, Original);
        File.CreateSymbolicLink(Target, "real.json"); // relative, as a dotfiles manager makes them

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.NotNull(new FileInfo(Target).LinkTarget);
        Assert.Equal(Replacement, File.ReadAllBytes(real));
        Assert.Equal(["real.json", "save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_KeepsTheFilePermissions()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        const UnixFileMode ownerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        File.WriteAllBytes(Target, Original);
        File.SetUnixFileMode(Target, ownerOnly);

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal(ownerOnly, File.GetUnixFileMode(Target));
    }

    [Fact]
    public void WriteAllBytes_ThroughAChainOfLinks_WritesWhereTheChainReallyEnds()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // outer -> dirlink/inner, dirlink -> real/sub, real/sub/inner -> ../x.json. The ".." has
        // to be taken from real/sub, where inner really is - not from dirlink's own folder.
        var sub = Directory.CreateDirectory(Path.Combine(_directory, "real", "sub")).FullName;
        var realFile = Path.Combine(_directory, "real", "x.json");
        File.WriteAllBytes(realFile, Original);
        Directory.CreateSymbolicLink(Path.Combine(_directory, "dirlink"), sub);
        File.CreateSymbolicLink(Path.Combine(sub, "inner"), Path.Combine("..", "x.json"));
        File.CreateSymbolicLink(Target, Path.Combine("dirlink", "inner"));

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal(Replacement, File.ReadAllBytes(realFile));
        Assert.Equal(Replacement, File.ReadAllBytes(Target));
        Assert.Equal(["dirlink", "real", "save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_ThroughALinkLoop_IsRefused()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.CreateSymbolicLink(Target, "other.json");
        File.CreateSymbolicLink(Path.Combine(_directory, "other.json"), "save.json");

        Assert.Throws<IOException>(() => AtomicFile.WriteAllBytes(Target, Replacement));

        Assert.Equal(["other.json", "save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_WhenTheSwapFails_RemovesItsTempFile()
    {
        // A directory where the file should be: the bytes are staged, then the swap fails.
        Directory.CreateDirectory(Target);

        Assert.ThrowsAny<Exception>(() => AtomicFile.WriteAllBytes(Target, Replacement));

        Assert.Equal(["save.json"], FileNames());
    }

    [Fact]
    public void WriteAllBytes_DoesNotUseAFixedTempName()
    {
        // The temp name used to be fixed per target, so a second writer's cleanup deleted the
        // first writer's file (issue #88). A writer mid-save is stood in for by its staging
        // file: this write must neither collide with it nor remove it.
        var othersTemp = Target + ".0123456789abcdef0123456789abcdef.pcedit-tmp";
        File.WriteAllBytes(othersTemp, Original);
        Directory.CreateDirectory(Target + ".pcedit-tmp");

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal(Replacement, File.ReadAllBytes(Target));
        Assert.Equal(Original, File.ReadAllBytes(othersTemp));
    }

    [Fact]
    public void WriteAllBytes_ClearsTempFilesAnInterruptedWriteLeftBehind()
    {
        // A crash mid-write leaves a staging file nothing will ever reuse, now that each has its
        // own name; one from long ago cannot belong to a write still in progress.
        var abandoned = Target + ".0123456789abcdef0123456789abcdef.pcedit-tmp";
        File.WriteAllBytes(abandoned, Original);
        File.SetLastWriteTimeUtc(abandoned, DateTime.UtcNow.AddDays(-2));
        var unrelated = Path.Combine(_directory, "other.json.0123456789abcdef0123456789abcdef.pcedit-tmp");
        File.WriteAllBytes(unrelated, Original);
        File.SetLastWriteTimeUtc(unrelated, DateTime.UtcNow.AddDays(-2));

        AtomicFile.WriteAllBytes(Target, Replacement);

        Assert.Equal([Path.GetFileName(unrelated), "save.json"], FileNames());
    }

    /// <summary>Makes the next write to <see cref="Target"/> fail without touching the file.</summary>
    private IDisposable BlockWrites()
    {
        if (OperatingSystem.IsWindows())
        {
            return new FileStream(Target, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        var writable = File.GetUnixFileMode(_directory);
        File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        return new Restore(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_directory, writable);
            }
        });
    }

    private sealed class Restore(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }
}
