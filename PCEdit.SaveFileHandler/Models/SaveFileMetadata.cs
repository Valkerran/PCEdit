using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record SaveFileMetadata
{
    public required string SaveDisplayName { get; init; }

    public required string PlanetId { get; init; }

    [JsonRequired]
    public bool UnlockedSpaceTrading { get; init; }

    [JsonRequired]
    public bool UnlockedOreExtrators { get; init; }

    [JsonRequired]
    public bool UnlockedTeleporters { get; init; }

    [JsonRequired]
    public bool UnlockedDrones { get; init; }

    [JsonRequired]
    public bool UnlockedAutocrafter { get; init; }

    [JsonRequired]
    public bool UnlockedEverything { get; init; }

    [JsonRequired]
    public bool FreeCraft { get; init; }

    [JsonRequired]
    public bool PreInterplanetarySave { get; init; }

    [JsonRequired]
    public bool RandomizeMineables { get; init; }

    [JsonRequired]
    public GameNumber ModifierTerraformationPace { get; init; }

    [JsonRequired]
    public GameNumber ModifierPowerConsumption { get; init; }

    [JsonRequired]
    public GameNumber ModifierGaugeDrain { get; init; }

    [JsonRequired]
    public GameNumber ModifierMeteoOccurence { get; init; }

    [JsonRequired]
    public GameNumber ModifierMultiplayerTerraformationFactor { get; init; }

    [JsonRequired]
    public bool Modded { get; init; }

    public required string Version { get; init; }

    public required string Mode { get; init; }

    public required string DyingConsequencesLabel { get; init; }

    public required string StartLocationLabel { get; init; }

    [JsonRequired]
    public int WorldSeed { get; init; }

    [JsonRequired]
    public bool HasPlayedIntro { get; init; }

    public required string GameStartLocation { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
