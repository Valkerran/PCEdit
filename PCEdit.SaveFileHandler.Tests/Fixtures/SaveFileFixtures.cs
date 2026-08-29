using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests.Fixtures;

/// <summary>
/// Builds <see cref="PlanetCrafterSaveFile"/> instances with a distinct, greppable marker value in
/// every property, so a round trip through the serializer can assert each value lands back in the
/// exact same property it started in. This pins down the section-order contract documented in
/// CLAUDE.md's "Architecture: the save-file format" table.
/// </summary>
internal static class SaveFileFixtures
{
    public static PlanetCrafterSaveFile CreateFullyPopulated()
    {
        return new PlanetCrafterSaveFile
        {
            Unlocks = new SaveFileUnlocks
            {
                TerraTokens = 111,
                AllTimeTerraTokens = 222,
                UnlockedGroups = "marker-unlockedGroups",
                OpenedInstanceSeed = 333,
                OpenedInstanceTimeLeft = 444
            },
            Terraformations =
            [
                new PlanetTerraformation
                {
                    PlanetId = "marker-terraformation-planetId",
                    UnitOxygenLevel = 1.1m,
                    UnitHeatLevel = 2.2m,
                    UnitPressureLevel = 3.3m,
                    UnitPlantsLevel = 4.4m,
                    UnitInsectsLevel = 5.5m,
                    UnitAnimalsLevel = 6.6m,
                    UnitPurificationLevel = 7.7m
                }
            ],
            Players =
            [
                new PlayerData
                {
                    Id = 1001,
                    Name = "marker-player-name",
                    InventoryId = 5,
                    EquipmentId = 6,
                    PlayerPosition = "1,2,3",
                    PlayerRotation = "4,5,6",
                    PlayerGaugeOxygen = 10.1m,
                    PlayerGaugeThirst = 20.2m,
                    PlayerGaugeHealth = 30.3m,
                    PlayerGaugeToxic = 40.4m,
                    Host = true,
                    PlanetId = "marker-player-planetId",
                    TotalCraftedObjects = 7,
                    TotalTerraTokenEarned = 8,
                    CameraView = 9
                }
            ],
            WorldObjects =
            [
                new WorldObject
                {
                    Id = 2001,
                    GId = "marker-worldobject-gid",
                    Position = "7,8,9",
                    Rotation = "10,11,12",
                    Planet = 12345,
                    LinkedInventoryId = 5,
                    PanelSettings = "marker-panelSettings",
                    Growth = 50,
                    LinkedInventoryGroups = "marker-liGrps",
                    SpawnedInstanceIds = "marker-siIds",
                    Color = "marker-color",
                    MineableCount = "13,14",
                    LinkedWorldObjectId = 2002,
                    Text = "marker-text"
                }
            ],
            Inventories =
            [
                new Inventory
                {
                    Id = 5,
                    WorldObjectIds = "2001",
                    Size = 12,
                    DemandGroups = "marker-demandGrps",
                    SupplyGroups = "marker-supplyGrps",
                    Priority = 3
                }
            ],
            Statistics = new SaveFileStatistics
            {
                CraftedObjects = 61,
                TotalSaveFileLoad = 62,
                TotalSaveFileTime = 63
            },
            ReadMessages =
            [
                new ReadMessage { StringId = "marker-readmessage-stringId", IsRead = true }
            ],
            StoryEvents =
            [
                new StoryEvent { StringId = "marker-storyevent-stringId" }
            ],
            Metadata = new SaveFileMetadata
            {
                SaveDisplayName = "marker-metadata-saveDisplayName",
                PlanetId = "marker-metadata-planetId",
                UnlockedSpaceTrading = true,
                UnlockedOreExtrators = true,
                UnlockedTeleporters = true,
                UnlockedDrones = true,
                UnlockedAutocrafter = true,
                UnlockedEverything = false,
                FreeCraft = false,
                PreInterplanetarySave = false,
                RandomizeMineables = true,
                ModifierTerraformationPace = 1.5m,
                ModifierPowerConsumption = 2.5m,
                ModifierGaugeDrain = 3.5m,
                ModifierMeteoOccurence = 4.5m,
                ModifierMultiplayerTerraformationFactor = 5.5m,
                Modded = false,
                Version = "marker-metadata-version",
                Mode = "marker-metadata-mode",
                DyingConsequencesLabel = "marker-metadata-dying",
                StartLocationLabel = "marker-metadata-startLocation",
                WorldSeed = 99,
                HasPlayedIntro = true,
                GameStartLocation = "marker-metadata-gameStartLocation"
            },
            ProceduralInstances =
            [
                new ProceduralInstance
                {
                    Owner = 71,
                    Planet = 72,
                    Index = 73,
                    Seed = 74,
                    Position = "marker-procedural-pos",
                    Rotation = "marker-procedural-rot",
                    WrecksWorldObjectsGenerated = true,
                    WorldObjectIdsGenerated = "marker-woIdsGenerated",
                    WorldObjectIdsDropped = "marker-woIdsDropped",
                    Version = 75
                }
            ]
        };
    }

    /// <summary>A minimally valid save with every list section empty, to exercise empty-section round-tripping.</summary>
    public static PlanetCrafterSaveFile CreateEmpty()
    {
        return new PlanetCrafterSaveFile
        {
            Unlocks = new SaveFileUnlocks { UnlockedGroups = "" },
            Statistics = new SaveFileStatistics(),
            Metadata = new SaveFileMetadata
            {
                SaveDisplayName = "Empty",
                PlanetId = "Prime",
                Version = "1",
                Mode = "Default",
                DyingConsequencesLabel = "Normal",
                StartLocationLabel = "Default",
                GameStartLocation = "Default"
            }
        };
    }
}
