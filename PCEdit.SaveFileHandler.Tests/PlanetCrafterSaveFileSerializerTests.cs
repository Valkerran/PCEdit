using PCEdit.SaveFileHandler.Tests.Fixtures;

namespace PCEdit.SaveFileHandler.Tests;

public sealed class PlanetCrafterSaveFileSerializerTests
{
    private readonly PlanetCrafterSaveFileSerializer _serializer = new(new JsonRecordSerializer());

    [Fact]
    public void Deserialize_NullContent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_FewerThanTenSections_ThrowsInvalidDataException()
    {
        var content = string.Join('@', Enumerable.Repeat("{}", 5));

        var exception = Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(content));

        Assert.Contains("10", exception.Message);
    }

    [Fact]
    public void Serialize_NullSaveFile_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _serializer.Serialize(null!));
    }

    [Fact]
    public void Serialize_ReproducesTheGameFraming()
    {
        var content = _serializer.Serialize(SaveFileFixtures.CreateFullyPopulated());

        // Leading CR, ten sections joined by "\r@\r", trailing "\r@" (no newline); records inside
        // a list section joined by "|\n". No BOM — that is the store's encoding concern.
        Assert.StartsWith("\r{", content);
        Assert.EndsWith("}\r@", content);
        Assert.DoesNotContain("\r\n", content);
        Assert.DoesNotContain("|\r", content);
        Assert.Equal(10, content.Split("\r@\r").Length);
    }

    [Fact]
    public void RoundTrip_EveryFieldLandsBackInTheSamePropertyItStartedIn()
    {
        var original = SaveFileFixtures.CreateFullyPopulated();

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        // Section 0: Unlocks
        Assert.Equal(original.Unlocks.TerraTokens, roundTripped.Unlocks.TerraTokens);
        Assert.Equal(original.Unlocks.AllTimeTerraTokens, roundTripped.Unlocks.AllTimeTerraTokens);
        Assert.Equal(original.Unlocks.UnlockedGroups, roundTripped.Unlocks.UnlockedGroups);
        Assert.Equal(original.Unlocks.OpenedInstanceSeed, roundTripped.Unlocks.OpenedInstanceSeed);
        Assert.Equal(original.Unlocks.OpenedInstanceTimeLeft, roundTripped.Unlocks.OpenedInstanceTimeLeft);

        // Section 1: Terraformation
        var terraformation = Assert.Single(roundTripped.Terraformations);
        Assert.Equal(original.Terraformations[0].PlanetId, terraformation.PlanetId);
        Assert.Equal(original.Terraformations[0].UnitOxygenLevel, terraformation.UnitOxygenLevel);

        // Section 2: Players
        var player = Assert.Single(roundTripped.Players);
        Assert.Equal(original.Players[0].Id, player.Id);
        Assert.Equal(original.Players[0].Name, player.Name);
        Assert.Equal(original.Players[0].PlanetId, player.PlanetId);

        // Section 3: World objects
        var worldObject = Assert.Single(roundTripped.WorldObjects);
        Assert.Equal(original.WorldObjects[0].Id, worldObject.Id);
        Assert.Equal(original.WorldObjects[0].GId, worldObject.GId);
        Assert.Equal(original.WorldObjects[0].Position, worldObject.Position);
        Assert.Equal(original.WorldObjects[0].Rotation, worldObject.Rotation);
        Assert.Equal(original.WorldObjects[0].Planet, worldObject.Planet);
        Assert.Equal(original.WorldObjects[0].LinkedInventoryId, worldObject.LinkedInventoryId);
        Assert.Equal(original.WorldObjects[0].PanelSettings, worldObject.PanelSettings);
        Assert.Equal(original.WorldObjects[0].Growth, worldObject.Growth);
        Assert.Equal(original.WorldObjects[0].LinkedInventoryGroups, worldObject.LinkedInventoryGroups);
        Assert.Equal(original.WorldObjects[0].SpawnedInstanceIds, worldObject.SpawnedInstanceIds);
        Assert.Equal(original.WorldObjects[0].Color, worldObject.Color);
        Assert.Equal(original.WorldObjects[0].MineableCount, worldObject.MineableCount);
        Assert.Equal(original.WorldObjects[0].LinkedWorldObjectId, worldObject.LinkedWorldObjectId);
        Assert.Equal(original.WorldObjects[0].Text, worldObject.Text);

        // Section 4: Inventories
        var inventory = Assert.Single(roundTripped.Inventories);
        Assert.Equal(original.Inventories[0].Id, inventory.Id);
        Assert.Equal(original.Inventories[0].WorldObjectIds, inventory.WorldObjectIds);
        Assert.Equal(original.Inventories[0].Size, inventory.Size);
        Assert.Equal(original.Inventories[0].DemandGroups, inventory.DemandGroups);
        Assert.Equal(original.Inventories[0].SupplyGroups, inventory.SupplyGroups);
        Assert.Equal(original.Inventories[0].Priority, inventory.Priority);

        // Section 5: Statistics
        Assert.Equal(original.Statistics.CraftedObjects, roundTripped.Statistics.CraftedObjects);
        Assert.Equal(original.Statistics.TotalSaveFileLoad, roundTripped.Statistics.TotalSaveFileLoad);
        Assert.Equal(original.Statistics.TotalSaveFileTime, roundTripped.Statistics.TotalSaveFileTime);

        // Section 6: Read messages
        var readMessage = Assert.Single(roundTripped.ReadMessages);
        Assert.Equal(original.ReadMessages[0].StringId, readMessage.StringId);
        Assert.Equal(original.ReadMessages[0].IsRead, readMessage.IsRead);

        // Section 7: Story events
        var storyEvent = Assert.Single(roundTripped.StoryEvents);
        Assert.Equal(original.StoryEvents[0].StringId, storyEvent.StringId);

        // Section 8: Metadata
        Assert.Equal(original.Metadata.SaveDisplayName, roundTripped.Metadata.SaveDisplayName);
        Assert.Equal(original.Metadata.PlanetId, roundTripped.Metadata.PlanetId);
        Assert.Equal(original.Metadata.Version, roundTripped.Metadata.Version);

        // Section 9: Procedural instances
        var proceduralInstance = Assert.Single(roundTripped.ProceduralInstances);
        Assert.Equal(original.ProceduralInstances[0].Owner, proceduralInstance.Owner);
        Assert.Equal(original.ProceduralInstances[0].Position, proceduralInstance.Position);
        Assert.Equal(original.ProceduralInstances[0].WorldObjectIdsGenerated, proceduralInstance.WorldObjectIdsGenerated);
    }

    [Fact]
    public void RoundTrip_EmptyListSections_StayEmpty()
    {
        var original = SaveFileFixtures.CreateEmpty();

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Empty(roundTripped.Terraformations);
        Assert.Empty(roundTripped.Players);
        Assert.Empty(roundTripped.WorldObjects);
        Assert.Empty(roundTripped.Inventories);
        Assert.Empty(roundTripped.ReadMessages);
        Assert.Empty(roundTripped.StoryEvents);
        Assert.Empty(roundTripped.ProceduralInstances);
    }

    [Fact]
    public void Deserialize_MultipleRecordsInAListSection_AreAllParsed_IgnoringTrailingEmptyEntry()
    {
        var original = SaveFileFixtures.CreateEmpty();
        original.ReadMessages.Add(new Models.ReadMessage { StringId = "first", IsRead = true });
        original.ReadMessages.Add(new Models.ReadMessage { StringId = "second", IsRead = false });

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(2, roundTripped.ReadMessages.Count);
        Assert.Equal("first", roundTripped.ReadMessages[0].StringId);
        Assert.Equal("second", roundTripped.ReadMessages[1].StringId);
    }

    [Fact]
    public void Deserialize_MultipleTerraformationEntries_AreAllParsed()
    {
        var original = SaveFileFixtures.CreateEmpty();
        original.Terraformations.Add(new Models.PlanetTerraformation { PlanetId = "Prime" });
        original.Terraformations.Add(new Models.PlanetTerraformation { PlanetId = "Aria" });

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(2, roundTripped.Terraformations.Count);
        Assert.Equal("Prime", roundTripped.Terraformations[0].PlanetId);
        Assert.Equal("Aria", roundTripped.Terraformations[1].PlanetId);
    }

    [Fact]
    public void Deserialize_LeadingByteOrderMark_IsStrippedFromFirstSection()
    {
        var original = SaveFileFixtures.CreateEmpty();
        var content = ((char)0xFEFF) + _serializer.Serialize(original);

        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(original.Unlocks.UnlockedGroups, roundTripped.Unlocks.UnlockedGroups);
    }

    [Fact]
    public void RoundTrip_RealSampleSaveFile_ParsesAndReserializesConsistently()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Standard-2.json");
        var content = File.ReadAllText(path);

        var first = _serializer.Deserialize(content);
        var reserialized = _serializer.Serialize(first);
        var second = _serializer.Deserialize(reserialized);

        Assert.Equal(first.Unlocks.TerraTokens, second.Unlocks.TerraTokens);
        Assert.Equal(first.Terraformations.Count, second.Terraformations.Count);
        Assert.Equal(first.Players.Count, second.Players.Count);
        Assert.Equal(first.WorldObjects.Count, second.WorldObjects.Count);
        Assert.Equal(first.Inventories.Count, second.Inventories.Count);
        Assert.Equal(first.ReadMessages.Count, second.ReadMessages.Count);
        Assert.Equal(first.StoryEvents.Count, second.StoryEvents.Count);
        Assert.Equal(first.ProceduralInstances.Count, second.ProceduralInstances.Count);
        Assert.Equal(first.Metadata.SaveDisplayName, second.Metadata.SaveDisplayName);
        Assert.NotEmpty(first.Terraformations);
        Assert.NotEmpty(first.Players);
        Assert.NotEmpty(first.WorldObjects);
    }

    [Theory]
    [InlineData("Standard-2.json")]
    [InlineData("mini-save.json")]
    [InlineData("Humble-2.102.json")]
    [InlineData("Interplanetary-2.102.json")]
    public void RoundTrip_RealSampleSaveFile_PreservesEveryKeyAndValue(string fixtureName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fixtureName);
        var original = File.ReadAllText(path);

        var reserialized = _serializer.Serialize(_serializer.Deserialize(original));

        var differences = JsonSaveFileComparer.Diff(original, reserialized);

        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    [Theory]
    [InlineData("Standard-2.json")]
    [InlineData("mini-save.json")]
    [InlineData("Humble-2.102.json")]
    [InlineData("Interplanetary-2.102.json")]
    public void RoundTrip_RealSampleSaveFile_ReserializesCharacterForCharacter(string fixtureName)
    {
        // File.ReadAllText strips the BOM, and Serialize does not emit one (the store's encoding
        // adds it back), so this compares everything after the BOM: framing, key order, values.
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fixtureName);
        var original = File.ReadAllText(path);

        var reserialized = _serializer.Serialize(_serializer.Deserialize(original));

        Assert.Equal(original, reserialized);
    }
}
