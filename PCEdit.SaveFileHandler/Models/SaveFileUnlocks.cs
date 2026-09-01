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
    /// Whether the player has paused the logistics drones. Added by game version 2.102; nullable
    /// (not <see cref="bool"/>) so a pre-2.102 save that never carried the key does not gain a
    /// <c>"logisticsPaused":false</c> on save — <c>JsonIgnoreCondition.WhenWritingNull</c> omits it.
    /// </summary>
    public bool? LogisticsPaused { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
