using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed class ProceduralInstance
{
    public int Owner { get; init; }

    public int Planet { get; init; }

    public int Index { get; init; }

    public int Seed { get; init; }

    [JsonPropertyName("pos")]
    public required string Position { get; init; }

    [JsonPropertyName("rot")]
    public required string Rotation { get; init; }

    [JsonPropertyName("wrecksWOGenerated")]
    public bool WrecksWorldObjectsGenerated { get; init; }

    [JsonPropertyName("woIdsGenerated")]
    public required string WorldObjectIdsGenerated { get; init; }

    [JsonPropertyName("woIdsDropped")]
    public required string WorldObjectIdsDropped { get; init; }

    public int Version { get; init; }
}
