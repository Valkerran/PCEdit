namespace PCEdit.SaveFileHandler.Models;

public sealed class PlayerData
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
}
