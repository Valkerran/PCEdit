using System.Text.Json.Serialization;

namespace PCEdit.SaveFileHandler.Models;

public sealed class Inventory
{
    public int Id { get; init; }

    [JsonPropertyName("woIds")]
    public required string WorldObjectIds { get; init; }

    public int Size { get; init; }

    public string? DemandGroups { get; init; }

    public string? SupplyGroups { get; init; }

    public int? Priority { get; init; }
}
