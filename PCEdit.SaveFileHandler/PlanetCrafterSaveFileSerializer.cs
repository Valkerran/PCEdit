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

    private readonly IJsonRecordSerializer _jsonRecordSerializer 
        = jsonRecordSerializer ?? throw new ArgumentNullException(nameof(jsonRecordSerializer));

    public PlanetCrafterSaveFile Deserialize(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var sections = content.Split(SectionDelimiter);
        if (sections.Length < RequiredSectionCount)
        {
            throw new InvalidDataException(
                $"A Planet Crafter save file requires at least {RequiredSectionCount} sections; found {sections.Length}.");
        }

        sections[0] = sections[0].TrimStart('\uFEFF');

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
