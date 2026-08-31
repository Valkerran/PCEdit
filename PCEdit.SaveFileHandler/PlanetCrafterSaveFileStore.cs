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

        var encoding = FileStartsWithBom(path) ? Utf8WithBom : Utf8WithoutBom;
        File.WriteAllText(path, _serializer.Serialize(saveFile), encoding);
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
