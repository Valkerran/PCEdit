using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.Fixtures;

/// <summary>
/// Builds a small, self-consistent <see cref="PlanetCrafterSaveFile"/> for exercising
/// <c>SaveFileWorkspace</c> and <c>InventoryEditor</c> without touching disk.
///
/// Layout:
///  - Player "Alice" (Id=1): inventory 10 (holds items 200, 201; size 5), equipment 11 (empty; size 2)
///  - Player "Bob" (Id=2): inventory 20 (empty; size 1 -- deliberately full-able), equipment 21 (empty; size 2)
///  - World object 100 ("StorageContainer") links to inventory 30 (holds item 202; size 3)
///  - Inventory 99 has no owning player or container (exercises the label fallback)
/// </summary>
internal static class WorkspaceFixtures
{
    public static PlanetCrafterSaveFile Create()
    {
        return new PlanetCrafterSaveFile
        {
            Unlocks = new SaveFileUnlocks { TerraTokens = 100, AllTimeTerraTokens = 100, UnlockedGroups = "" },
            Terraformations =
            [
                new PlanetTerraformation { PlanetId = "Prime" }
            ],
            Statistics = new SaveFileStatistics(),
            Metadata = new SaveFileMetadata
            {
                SaveDisplayName = "Test Save",
                PlanetId = "Prime",
                Version = "2.102",
                Mode = "Default",
                DyingConsequencesLabel = "Normal",
                StartLocationLabel = "Default",
                GameStartLocation = "Default"
            },
            Players =
            [
                new PlayerData
                {
                    Id = 1,
                    Name = "Alice",
                    InventoryId = 10,
                    EquipmentId = 11,
                    PlayerPosition = "0,0,0",
                    PlayerRotation = "0,0,0",
                    PlanetId = "Prime",
                    TotalTerraTokenEarned = 0
                },
                new PlayerData
                {
                    Id = 2,
                    Name = "Bob",
                    InventoryId = 20,
                    EquipmentId = 21,
                    PlayerPosition = "0,0,0",
                    PlayerRotation = "0,0,0",
                    PlanetId = "Prime",
                    TotalTerraTokenEarned = 0
                }
            ],
            WorldObjects =
            [
                new WorldObject { Id = 100, GId = "StorageContainer", LinkedInventoryId = 30 },
                new WorldObject { Id = 200, GId = "Item200" },
                new WorldObject { Id = 201, GId = "Item201" },
                new WorldObject { Id = 202, GId = "Item202" }
            ],
            Inventories =
            [
                new Inventory { Id = 10, WorldObjectIds = "200,201", Size = 5 },
                new Inventory { Id = 11, WorldObjectIds = "", Size = 2 },
                new Inventory { Id = 20, WorldObjectIds = "", Size = 1 },
                new Inventory { Id = 21, WorldObjectIds = "", Size = 2 },
                new Inventory { Id = 30, WorldObjectIds = "202", Size = 3 },
                new Inventory { Id = 99, WorldObjectIds = "", Size = 1 }
            ]
        };
    }

    /// <summary>
    /// <see cref="Create"/> extended to two worlds: Alice stays on "Prime", Bob moves to
    /// "Aqualis", and the storage container (world object 100 → inventory 30) is placed on
    /// "Aqualis" via its <c>planet</c> hash. Inventory 99 stays orphaned (no world).
    /// </summary>
    public static PlanetCrafterSaveFile CreateMultiWorld()
    {
        var save = Create();
        save.Terraformations.Add(new PlanetTerraformation { PlanetId = "Aqualis" });

        var bob = save.Players.FindIndex(p => p.Id == 2);
        save.Players[bob] = save.Players[bob] with { PlanetId = "Aqualis" };

        var container = save.WorldObjects.FindIndex(w => w.Id == 100);
        save.WorldObjects[container] = save.WorldObjects[container] with { Planet = PlanetHash.Of("Aqualis") };

        return save;
    }
}
