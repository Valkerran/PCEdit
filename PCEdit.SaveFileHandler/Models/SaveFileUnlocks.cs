using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record SaveFileUnlocks
{
    public int TerraTokens { get; init; }

    public int AllTimeTerraTokens { get; init; }

    public required string UnlockedGroups { get; init; }

    public int OpenedInstanceSeed { get; init; }

    public int OpenedInstanceTimeLeft { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
