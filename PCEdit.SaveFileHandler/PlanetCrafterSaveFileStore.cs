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
    // game's own output. File.ReadAllText auto-detects and strips the BOM on the way in.
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // The edit goes to a sibling temp file that is then swapped in, so an interrupted write
    // cannot leave a truncated save behind: File.WriteAllText truncates the target before it
    // writes, so a crash, power loss or full disk in between destroyed the original with no
    // copy anywhere (issue #36). Sibling, because the swap is only atomic within one volume.
    // The suffix also keeps the transient file out of the *.json the game enumerates.
    private const string TempSuffix = ".pcedit-tmp";

    private readonly IPlanetCrafterSaveFileSerializer _serializer
        = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public PlanetCrafterSaveFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _serializer.Deserialize(File.ReadAllText(path));
    }

    public void Save(string path, PlanetCrafterSaveFile saveFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(saveFile);

        // Probe the file already on disk, and serialize, before anything is written - neither may
        // be able to fail once the target has been touched. The probe in particular has to read
        // the original, not the temp file, or the BOM-less Game Pass path regresses.
        var encoding = FileStartsWithBom(path) ? Utf8WithBom : Utf8WithoutBom;
        var content = _serializer.Serialize(saveFile);

        var tempPath = path + TempSuffix;
        try
        {
            WriteThrough(tempPath, content, encoding);

            // Atomic on both platforms (MoveFileEx with REPLACE_EXISTING on Windows, rename on
            // Unix), and handles a destination that does not exist yet, so "Save As" needs no
            // separate path. The save file is therefore either wholly the old one or wholly the
            // new one - never a half-written mixture.
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Writes the content and forces it to the physical disk before returning. Without the flush
    /// the swap can complete while the new bytes are still only in the OS cache, so a power loss
    /// leaves the save present but empty - the very outcome the temp-and-swap exists to prevent.
    /// </summary>
    private static void WriteThrough(string path, string content, Encoding encoding)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, encoding);

        writer.Write(content);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    /// <summary>Clears the temp file after a failed save. The original is already intact by then,
    /// so a failure to clean up must not mask the error that actually mattered.</summary>
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

    // Only the Steam save carries a UTF-8 BOM. A file that does not exist yet is treated as
    // BOM-prefixed so a brand-new save matches the Steam game's output.
    private static bool FileStartsWithBom(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        Span<byte> head = stackalloc byte[3];
        using var stream = File.OpenRead(path);
        var read = stream.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        return read == head.Length && head.SequenceEqual(Utf8Bom);
    }
}
