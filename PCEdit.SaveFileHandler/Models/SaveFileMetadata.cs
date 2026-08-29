using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record SaveFileMetadata
{
    public required string SaveDisplayName { get; init; }

    public required string PlanetId { get; init; }

    public bool UnlockedSpaceTrading { get; init; }

    public bool UnlockedOreExtrators { get; init; }

    public bool UnlockedTeleporters { get; init; }

    public bool UnlockedDrones { get; init; }

    public bool UnlockedAutocrafter { get; init; }

    public bool UnlockedEverything { get; init; }

    public bool FreeCraft { get; init; }

    public bool PreInterplanetarySave { get; init; }

    public bool RandomizeMineables { get; init; }

    public decimal ModifierTerraformationPace { get; init; }

    public decimal ModifierPowerConsumption { get; init; }

    public decimal ModifierGaugeDrain { get; init; }

    public decimal ModifierMeteoOccurence { get; init; }

    public decimal ModifierMultiplayerTerraformationFactor { get; init; }

    public bool Modded { get; init; }

    public required string Version { get; init; }

    public required string Mode { get; init; }

    public required string DyingConsequencesLabel { get; init; }

    public required string StartLocationLabel { get; init; }

    public int WorldSeed { get; init; }

    public bool HasPlayedIntro { get; init; }

    public required string GameStartLocation { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
