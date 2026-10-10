using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record ProceduralInstance
{
    [JsonRequired]
    public int Owner { get; init; }

    [JsonRequired]
    public int Planet { get; init; }

    [JsonRequired]
    public int Index { get; init; }

    [JsonRequired]
    public int Seed { get; init; }

    [JsonPropertyName("pos")]
    public required string Position { get; init; }

    [JsonPropertyName("rot")]
    public required string Rotation { get; init; }

    [JsonRequired]
    [JsonPropertyName("wrecksWOGenerated")]
    public bool WrecksWorldObjectsGenerated { get; init; }

    [JsonPropertyName("woIdsGenerated")]
    public required string WorldObjectIdsGenerated { get; init; }

    [JsonPropertyName("woIdsDropped")]
    public required string WorldObjectIdsDropped { get; init; }

    [JsonRequired]
    public int Version { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
