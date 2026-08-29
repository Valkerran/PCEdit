using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record PlanetTerraformation
{
    public required string PlanetId { get; init; }

    public decimal UnitOxygenLevel { get; init; }

    public decimal UnitHeatLevel { get; init; }

    public decimal UnitPressureLevel { get; init; }

    public decimal UnitPlantsLevel { get; init; }

    public decimal UnitInsectsLevel { get; init; }

    public decimal UnitAnimalsLevel { get; init; }

    public decimal UnitPurificationLevel { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
