using System.Text;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public sealed class PlanetCrafterSaveFileStore(IPlanetCrafterSaveFileSerializer serializer)
    : IPlanetCrafterSaveFileStore
{
    // The Steam build writes the save as UTF-8 *with* a leading BOM; the Xbox / PC Game Pass (WGS)
    // build writes it *without* one, starting straight at the '\r' framing byte. Prepending a BOM
    // the file never had makes the game reject the save with a "file error", so Save preserves
    // whatever framing the file already on disk has instead of forcing a BOM (issue #12).
    // A path with no existing file (new save / "Save As") defaults to a BOM, matching the Steam
    // game's own output. Load strips a leading BOM on the way in (see DecodeUtf8).
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // Throws on a byte that is not UTF-8, rather than substituting U+FFFD: a substituted
    // character would be written back over the original on the next save (issue #77).
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IPlanetCrafterSaveFileSerializer _serializer
        = serializer ?? throw new ArgumentNullException(nameof(serializer));

    // Whether each file this store has loaded began with a BOM, by full path. Only consulted
    // when the file is no longer there to probe: see FileStartsWithBom.
    private readonly Dictionary<string, bool> _loadedWithBom = new(StringComparer.Ordinal);

    public PlanetCrafterSaveFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var bytes = File.ReadAllBytes(path);
        _loadedWithBom[Path.GetFullPath(path)] = bytes.AsSpan().StartsWith(Utf8Bom);
        return _serializer.Deserialize(DecodeUtf8(bytes));
    }

    public string? Fingerprint(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // The real file: a link's own size and time do not change when what it points at does.
        var info = new FileInfo(RealPath.Of(path));
        return info.Exists ? $"{info.Length}:{info.LastWriteTimeUtc.Ticks}" : null;
    }

    /// <summary>The file's bytes as text, minus a UTF-8 BOM. Anything that is not UTF-8 - including a
    /// UTF-16 or UTF-32 file, whose BOM is itself invalid UTF-8 - is refused.</summary>
    private static string DecodeUtf8(byte[] bytes)
    {
        var start = bytes.AsSpan().StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
        try
        {
            return StrictUtf8.GetString(bytes, start, bytes.Length - start);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("The save file is not valid UTF-8 text.", exception);
        }
    }

    public void Save(string path, PlanetCrafterSaveFile saveFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(saveFile);

        Write(path, framingFrom: path, saveFile);
    }

    public void SaveCopy(string sourcePath, string targetPath, PlanetCrafterSaveFile saveFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(saveFile);

        if (SameFile(sourcePath, targetPath))
        {
            throw new ArgumentException("A copy must not overwrite the save it was made from.", nameof(targetPath));
        }

        Write(targetPath, framingFrom: sourcePath, saveFile);
    }

    /// <summary>Whether two paths name the same file once every symbolic link is resolved
    /// (issue #78) - Windows and macOS file systems ignore case by default, Linux does not.</summary>
    private static bool SameFile(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(RealPath.Of(first), RealPath.Of(second), comparison);
    }

    /// <summary>
    /// Serializes the save to <paramref name="path"/> with the framing of the file at
    /// <paramref name="framingFrom"/>: a BOM only if that file has one, or does not exist yet.
    /// </summary>
    private void Write(string path, string framingFrom, PlanetCrafterSaveFile saveFile)
    {
        // Probe the file already on disk, and serialize, before anything is written - neither may
        // be able to fail once the target has been touched. The probe has to read the original,
        // not a staging file, or the BOM-less Game Pass path regresses. The write itself cannot
        // leave a truncated save behind (issue #36): see AtomicFile.
        var encoding = FileStartsWithBom(framingFrom) ? Utf8WithBom : Utf8WithoutBom;
        var content = _serializer.Serialize(saveFile);

        AtomicFile.WriteAllBytes(path, [.. encoding.GetPreamble(), .. encoding.GetBytes(content)]);
    }

    // Only the Steam save carries a UTF-8 BOM. A file that does not exist yet is treated as
    // BOM-prefixed so a brand-new save matches the Steam game's output - unless this store loaded
    // it and it has been removed since, when it is written back as it was: a Game Pass save
    // that gained a BOM would be rejected by the game.
    private bool FileStartsWithBom(string path)
    {
        if (!File.Exists(path))
        {
            return _loadedWithBom.GetValueOrDefault(Path.GetFullPath(path), true);
        }

        Span<byte> head = stackalloc byte[3];
        using var stream = File.OpenRead(path);
        var read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        return read == head.Length && head.SequenceEqual(Utf8Bom);
    }
}
