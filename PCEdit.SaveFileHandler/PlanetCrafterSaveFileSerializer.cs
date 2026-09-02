using System.Text.RegularExpressions;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler;

public sealed class PlanetCrafterSaveFileSerializer(IJsonRecordSerializer jsonRecordSerializer)
    : IPlanetCrafterSaveFileSerializer
{
    private const char SectionDelimiter = '@';
    private const char RecordDelimiter = '|';
    private const int RequiredSectionCount = 10;

    // The game frames the file as: a leading CR, the 10 sections joined by "\r@\r", and a
    // trailing "\r@" (no newline); records inside a list section are joined by "|\n". The file
    // also carries a UTF-8 BOM, which is an encoding concern handled by the store, not here.
    // Reproducing this framing byte-for-byte keeps a load→save diff of an otherwise-unchanged
    // save empty, so a user can see exactly what an edit changed.
    private const string FilePrefix = "\r";
    private const string FileSuffix = "\r@";
    private const string SectionSeparator = "\r@\r";
    private const string RecordSeparator = "|\n";

    // The '@' separating two sections is always bracketed by the framing line breaks
    // ("\r@\r"). Matching it that way - rather than splitting on every '@' in the file -
    // leaves an '@' inside a JSON string alone, which is exactly where player-typed free text
    // puts one (a container/sign label, a player name). Splitting on the bare character
    // truncated those saves mid-string, and in the one placement where the prefix stayed valid
    // JSON it dropped the remainder with no error at all (issue #38).
    //
    // Lookaround rather than a consuming match, so two separators sharing a line break - what
    // an empty section between them looks like - are both still found.
    private static readonly Regex SectionSplitter = new(@"(?<=[\r\n])@(?=[\r\n])", RegexOptions.Compiled);

    private readonly IJsonRecordSerializer _jsonRecordSerializer 
        = jsonRecordSerializer ?? throw new ArgumentNullException(nameof(jsonRecordSerializer));

    public PlanetCrafterSaveFile Deserialize(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var sections = SectionSplitter.Split(StripTerminator(content.TrimStart('\uFEFF')));
        if (sections.Length < RequiredSectionCount)
        {
            throw new InvalidDataException(
                $"A Planet Crafter save file requires {RequiredSectionCount} sections; found {sections.Length}.");
        }

        // Reading past extra sections would drop them silently on the next save - which is how a
        // format change in a future game version would eat a player's data (issue #20).
        if (sections.Length > RequiredSectionCount)
        {
            throw new InvalidDataException(
                $"This save file has more sections ({sections.Length}) than the {RequiredSectionCount} "
                + "this build of PCEdit understands; it is likely from a newer version of the game.");
        }

        return new PlanetCrafterSaveFile
        {
            Unlocks = _jsonRecordSerializer.Deserialize<SaveFileUnlocks>(sections[0], 0),
            Terraformations = DeserializeRecords<PlanetTerraformation>(sections[1], 1),
            Players = DeserializeRecords<PlayerData>(sections[2], 2),
            WorldObjects = DeserializeRecords<WorldObject>(sections[3], 3),
            Inventories = DeserializeRecords<Inventory>(sections[4], 4),
            Statistics = _jsonRecordSerializer.Deserialize<SaveFileStatistics>(sections[5], 5),
            ReadMessages = DeserializeRecords<ReadMessage>(sections[6], 6),
            StoryEvents = DeserializeRecords<StoryEvent>(sections[7], 7),
            Metadata = _jsonRecordSerializer.Deserialize<SaveFileMetadata>(sections[8], 8),
            ProceduralInstances = DeserializeRecords<ProceduralInstance>(sections[9], 9)
        };
    }

    public string Serialize(PlanetCrafterSaveFile saveFile)
    {
        ArgumentNullException.ThrowIfNull(saveFile);

        var sections = new[]
        {
            _jsonRecordSerializer.Serialize(saveFile.Unlocks),
            SerializeRecords(saveFile.Terraformations),
            SerializeRecords(saveFile.Players),
            SerializeRecords(saveFile.WorldObjects),
            SerializeRecords(saveFile.Inventories),
            _jsonRecordSerializer.Serialize(saveFile.Statistics),
            SerializeRecords(saveFile.ReadMessages),
            SerializeRecords(saveFile.StoryEvents),
            _jsonRecordSerializer.Serialize(saveFile.Metadata),
            SerializeRecords(saveFile.ProceduralInstances)
        };

        return FilePrefix + string.Join(SectionSeparator, sections) + FileSuffix;
    }

    /// <summary>
    /// Removes the file's trailing "\r@" terminator. That final '@' is the one framing
    /// character with no line break after it, so <see cref="SectionSplitter"/> would
    /// otherwise leave it glued to the last section and make that section invalid JSON.
    /// Only whitespace may follow it.
    /// </summary>
    private static string StripTerminator(string content)
    {
        var lastDelimiter = content.LastIndexOf(SectionDelimiter);

        return lastDelimiter >= 0 && content.AsSpan(lastDelimiter + 1).IsWhiteSpace()
            ? content[..lastDelimiter]
            : content;
    }

    private List<T> DeserializeRecords<T>(string section, int sectionIndex)
    {
        return section
            .Split(RecordDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(record => _jsonRecordSerializer.Deserialize<T>(record, sectionIndex))
            .ToList();
    }

    private string SerializeRecords<T>(IEnumerable<T> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return string.Join(
            RecordSeparator,
            records.Select(_jsonRecordSerializer.Serialize));
    }
}
