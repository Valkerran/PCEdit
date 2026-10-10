using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record PlanetTerraformation
{
    public required string PlanetId { get; init; }

    public GameNumber UnitOxygenLevel { get; init; }

    public GameNumber UnitHeatLevel { get; init; }

    public GameNumber UnitPressureLevel { get; init; }

    public GameNumber UnitPlantsLevel { get; init; }

    public GameNumber UnitInsectsLevel { get; init; }

    public GameNumber UnitAnimalsLevel { get; init; }

    public GameNumber UnitPurificationLevel { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
