using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record Inventory
{
    public int Id { get; init; }

    [JsonPropertyName("woIds")]
    public required string WorldObjectIds { get; init; }

    public int Size { get; init; }

    [JsonPropertyName("demandGrps")]
    public string? DemandGroups { get; init; }

    [JsonPropertyName("supplyGrps")]
    public string? SupplyGroups { get; init; }

    public int? Priority { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
