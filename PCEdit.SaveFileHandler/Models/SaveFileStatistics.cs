using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record SaveFileStatistics
{
    [JsonRequired]
    public int CraftedObjects { get; init; }

    [JsonRequired]
    public int TotalSaveFileLoad { get; init; }

    [JsonRequired]
    public int TotalSaveFileTime { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
