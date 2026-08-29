using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record WorldObject
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

    /// <summary>
    /// Mineable amount on resource nodes (e.g. <c>GenerationGroupVein</c>), written by the game
    /// as a <c>"remaining,total"</c> pair. Absent on most world objects.
    /// </summary>
    [JsonPropertyName("count")]
    public string? MineableCount { get; init; }

    /// <summary>
    /// Id of another <see cref="WorldObject"/> this one is bound to (e.g. a
    /// <c>ToxicWaterCollector</c> → its source vein). Absent on most world objects.
    /// </summary>
    [JsonPropertyName("linkedWo")]
    public int? LinkedWorldObjectId { get; init; }

    /// <summary>
    /// Free text the player typed on the object (container / sign label). Absent on most world objects.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
