using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

// The game does not write world-object keys in a consistent order (the same field can appear
// before or after another between records), so matching it with a fixed property order is
// impossible. WorldObjectConverter instead records the exact key order each record was read
// with and replays it on write, so a load→save leaves untouched records byte-identical.
[JsonConverter(typeof(WorldObjectConverter))]
public sealed record WorldObject
{
    public int Id { get; init; }

    public required string GId { get; init; }

    [JsonPropertyName("liId")]
    public int? LinkedInventoryId { get; init; }

    [JsonPropertyName("liGrps")]
    public string? LinkedInventoryGroups { get; init; }

    [JsonPropertyName("siIds")]
    public string? SpawnedInstanceIds { get; init; }

    [JsonPropertyName("pos")]
    public string? Position { get; init; }

    [JsonPropertyName("rot")]
    public string? Rotation { get; init; }

    public int? Planet { get; init; }

    [JsonPropertyName("grwth")]
    public int? Growth { get; init; }

    /// <summary>
    /// Mineable amount on resource nodes (e.g. <c>GenerationGroupVein</c>), written by the game
    /// as a <c>"remaining,total"</c> pair. Absent on most world objects.
    /// </summary>
    [JsonPropertyName("count")]
    public string? MineableCount { get; init; }

    public string? Color { get; init; }

    [JsonPropertyName("pnls")]
    public string? PanelSettings { get; init; }

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
    /// Any JSON keys the game writes that this model does not name (e.g. <c>trtInd</c>,
    /// <c>trtVal</c>, <c>set</c>, <c>hunger</c>, <c>liPlanet</c>) — captured verbatim and
    /// re-emitted in their original position so a round-trip preserves them.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? ExtensionData { get; init; }

    /// <summary>
    /// The key order this record was read with, replayed on write. Null for objects built in
    /// code (they serialize in the property order declared above).
    /// </summary>
    public IReadOnlyList<string>? KeyOrder { get; init; }
}
