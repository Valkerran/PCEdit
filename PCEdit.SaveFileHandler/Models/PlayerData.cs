using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed record PlayerData
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    [JsonRequired]
    public int InventoryId { get; init; }

    [JsonRequired]
    public int EquipmentId { get; init; }

    public required string PlayerPosition { get; init; }

    public required string PlayerRotation { get; init; }

    [JsonRequired]
    public GameNumber PlayerGaugeOxygen { get; init; }

    [JsonRequired]
    public GameNumber PlayerGaugeThirst { get; init; }

    [JsonRequired]
    public GameNumber PlayerGaugeHealth { get; init; }

    [JsonRequired]
    public GameNumber PlayerGaugeToxic { get; init; }

    [JsonRequired]
    public bool Host { get; init; }

    public required string PlanetId { get; init; }

    [JsonRequired]
    public int TotalCraftedObjects { get; init; }

    [JsonRequired]
    public int TotalTerraTokenEarned { get; init; }

    [JsonRequired]
    public int CameraView { get; init; }

    /// <summary>
    /// Any JSON keys the game writes that this model does not name — captured so a
    /// load→save round-trip preserves them instead of silently dropping them.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
