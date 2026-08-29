using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record PlayerData
{
    public long Id { get; init; }

    public required string Name { get; init; }

    public int InventoryId { get; init; }

    public int EquipmentId { get; init; }

    public required string PlayerPosition { get; init; }

    public required string PlayerRotation { get; init; }

    public decimal PlayerGaugeOxygen { get; init; }

    public decimal PlayerGaugeThirst { get; init; }

    public decimal PlayerGaugeHealth { get; init; }

    public decimal PlayerGaugeToxic { get; init; }

    public bool Host { get; init; }

    public required string PlanetId { get; init; }

    public int TotalCraftedObjects { get; init; }

    public int TotalTerraTokenEarned { get; init; }

    public int CameraView { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
