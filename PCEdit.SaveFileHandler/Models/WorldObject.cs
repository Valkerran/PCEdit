using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed class WorldObject
{
    public int Id { get; init; }

    public required string GId { get; init; }

    [JsonPropertyName("pos")]
    public string? Position { get; init; }

    [JsonPropertyName("rot")]
    public string? Rotation { get; init; }

    public int? Planet { get; init; }

    [JsonPropertyName("liId")]
    public int? LinkedInventoryId { get; init; }

    [JsonPropertyName("pnls")]
    public string? PanelSettings { get; init; }

    [JsonPropertyName("grwth")]
    public int? Growth { get; init; }

    [JsonPropertyName("liGrps")]
    public string? LinkedInventoryGroups { get; init; }

    [JsonPropertyName("siIds")]
    public string? SpawnedInstanceIds { get; init; }

    public string? Color { get; init; }
}
