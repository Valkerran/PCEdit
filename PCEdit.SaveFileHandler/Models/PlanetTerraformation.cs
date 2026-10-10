using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record PlanetTerraformation
{
    public required string PlanetId { get; init; }

    [JsonRequired]
    public GameNumber UnitOxygenLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitHeatLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitPressureLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitPlantsLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitInsectsLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitAnimalsLevel { get; init; }

    [JsonRequired]
    public GameNumber UnitPurificationLevel { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
