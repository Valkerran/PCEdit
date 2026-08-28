namespace PCEdit.SaveFileHandler.Models;

public sealed class PlanetCrafterSaveFile
{
    public required SaveFileUnlocks Unlocks { get; init; }

    public List<PlanetTerraformation> Terraformations { get; init; } = [];

    public List<PlayerData> Players { get; init; } = [];

    public List<WorldObject> WorldObjects { get; init; } = [];

    public List<Inventory> Inventories { get; init; } = [];

    public required SaveFileStatistics Statistics { get; init; }

    public List<ReadMessage> ReadMessages { get; init; } = [];

    public List<StoryEvent> StoryEvents { get; init; } = [];

    public required SaveFileMetadata Metadata { get; init; }

    public List<ProceduralInstance> ProceduralInstances { get; init; } = [];
}
