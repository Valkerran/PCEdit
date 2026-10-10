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
    public void Deserialize_BadRecord_NamesTheSectionRecordAndKey()
    {
        var original = SaveFileFixtures.CreateEmpty();
        original.Inventories.Add(new Models.Inventory { Id = 1, WorldObjectIds = "", Size = 1 });
        original.Inventories.Add(new Models.Inventory { Id = 2, WorldObjectIds = "", Size = 1 });
        var content = _serializer.Serialize(original).Replace("\"id\":2", "\"id\":\"two\"", StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(content));

        Assert.Contains("section 4", exception.Message);
        Assert.Contains("record 2", exception.Message);
        Assert.Contains("$.id", exception.Message);
        Assert.DoesNotContain("LineNumber", exception.Message);
        Assert.DoesNotContain("BytePositionInLine", exception.Message);
        Assert.EndsWith("Path: $.id.", exception.Message);
    }

    [Fact]
    public void Deserialize_BadWorldObjectKey_NamesTheKey()
    {
        var original = SaveFileFixtures.CreateEmpty();
        original.WorldObjects.Add(new Models.WorldObject { Id = 1, GId = "Iron" });
        var content = _serializer.Serialize(original).Replace("\"id\":1", "\"id\":\"one\"", StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(content));

        Assert.Contains("record 1", exception.Message);
        Assert.Contains("\"id\"", exception.Message);
        Assert.EndsWith("wrong type.", exception.Message);
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
    [InlineData("Humble-2.103.json")]
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
    [InlineData("Humble-2.103.json")]
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

    [Theory]
    [InlineData("Ore@Base")]
    [InlineData("@")]
    [InlineData("a@b@c")]
    public void RoundTrip_ContainerLabelContainingTheSectionDelimiter_IsPreserved(string label)
    {
        // '@' frames the sections, but a player can type one into a container or sign label.
        // Splitting on the bare character truncated the save mid-string (issue #38).
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = label };

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(label, Assert.Single(roundTripped.WorldObjects).Text);
        Assert.Equal(content, _serializer.Serialize(roundTripped));
    }

    [Theory]
    [InlineData("at@sign")]
    [InlineData("@@@")]
    public void RoundTrip_PlayerNameContainingTheSectionDelimiter_IsPreserved(string name)
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.Players[0] = original.Players[0] with { Name = name };

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(name, Assert.Single(roundTripped.Players).Name);
        Assert.Equal(content, _serializer.Serialize(roundTripped));
    }

    // '|' joins records, but a player could type one into a label. Splitting on the bare
    // character made the whole save refuse to open (issue #71), the same class as #38.
    [Theory]
    [InlineData("a|b")]
    [InlineData("a|")]
    [InlineData("|")]
    [InlineData("a|\nb")]
    public void RoundTrip_ContainerLabelContainingTheRecordDelimiter_IsPreserved(string label)
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = label };

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(label, Assert.Single(roundTripped.WorldObjects).Text);
        Assert.Equal(content, _serializer.Serialize(roundTripped));
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void Deserialize_OtherLineEndingsInTheFraming_StillParse(string lineBreak)
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects.Add(original.WorldObjects[0] with { Id = 2002 });

        var serialized = _serializer.Serialize(original);
        Assert.Contains("|\n", serialized, StringComparison.Ordinal);

        var content = serialized
            .Replace("\r@\r", $"{lineBreak}@{lineBreak}", StringComparison.Ordinal)
            .Replace("|\n", $"|{lineBreak}", StringComparison.Ordinal);

        Assert.Equal(2, _serializer.Deserialize(content).WorldObjects.Count);
    }

    // The label sits beside a real record boundary, so both the '|' inside the string and the
    // '|' that joins the two records must be handled correctly in the same section.
    [Fact]
    public void RoundTrip_LabelContainingTheRecordDelimiter_BesideARealRecordBoundary_KeepsBothRecords()
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = "a|b" };
        original.WorldObjects.Add(original.WorldObjects[0] with { Id = 2002, Text = null });

        var content = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(content);

        Assert.Equal(2, roundTripped.WorldObjects.Count);
        Assert.Equal("a|b", roundTripped.WorldObjects[0].Text);
        Assert.Equal(content, _serializer.Serialize(roundTripped));
    }

    [Fact]
    public void Deserialize_MissingTrailingTerminator_StillParses()
    {
        var content = _serializer.Serialize(SaveFileFixtures.CreateFullyPopulated());

        Assert.Single(_serializer.Deserialize(content[..^2]).WorldObjects);
    }

    // Player-typed text must come back exactly as written: the default encoder escaped every
    // non-ASCII and HTML-sensitive character, so "Café & Bob's" came back as escapes (issue #70).
    [Theory]
    [InlineData("Café & Bob's +1")]
    [InlineData("<b>日本</b>")]
    [InlineData("rocket \U0001F680 \U00020000")]
    [InlineData("del\u007f sep\u2028")]
    public void RoundTrip_PlayerTypedText_IsWrittenWithoutEscapes(string text)
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = text };
        original.Players[0] = original.Players[0] with { Name = text };

        var content = _serializer.Serialize(original);

        Assert.Contains($"\"text\":\"{text}\"", content);
        Assert.Contains($"\"name\":\"{text}\"", content);
        Assert.Equal(content, _serializer.Serialize(_serializer.Deserialize(content)));
    }

    // A lone surrogate is not valid text; it must become U+FFFD, not truncate the string (issue #70).
    [Theory]
    [InlineData("lone#x", 0xD800, "lone\ufffdx")]
    [InlineData("lo#ne", 0xDC00, "lo\ufffdne")]
    [InlineData("end#", 0xD83D, "end\ufffd")]
    public void Serialize_LoneSurrogateInText_KeepsTheRestOfTheString(string template, int surrogate, string expected)
    {
        // Built at run time: a lone surrogate in attribute metadata is mangled before the test runs.
        var text = template.Replace('#', (char)surrogate);
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = text };

        var content = _serializer.Serialize(original);

        Assert.Contains($"\"text\":\"{expected}\"", content);
    }

    [Fact]
    public void RoundTrip_NonAsciiTextInAnUnknownKey_IsWrittenWithoutEscapes()
    {
        // A world object, because WorldObjectConverter replays unknown keys in place; the other
        // models append ExtensionData last, so an injected key would move and mask the result.
        var content = _serializer.Serialize(SaveFileFixtures.CreateFullyPopulated())
            .Replace("\"gId\":", "\"note\":\"Café 日本 \U0001F680\",\"gId\":", StringComparison.Ordinal);

        Assert.Contains("Café 日本 \U0001F680", content);
        Assert.Equal(content, _serializer.Serialize(_serializer.Deserialize(content)));
    }

    [Theory]
    [InlineData("quote\"", "quote\\\"")]
    [InlineData("back\\slash", "back\\\\slash")]
    [InlineData("tab\tline\n", "tab\\tline\\n")]
    [InlineData("bell\u0007", "bell\\u0007")]
    public void Serialize_CharactersJsonRequiresEscaped_AreStillEscaped(string text, string expected)
    {
        var original = SaveFileFixtures.CreateFullyPopulated();
        original.WorldObjects[0] = original.WorldObjects[0] with { Text = text };

        var content = _serializer.Serialize(original);

        Assert.Contains($"\"text\":\"{expected}\"", content);
        Assert.Equal(text, Assert.Single(_serializer.Deserialize(content).WorldObjects).Text);
    }

    [Fact]
    public void Deserialize_StrayDelimiterAtARecordBoundary_FailsInsteadOfDroppingRecordsSilently()
    {
        // The last section is the one place a stray '@' produced NO error at all: everything
        // before it is complete, valid JSON, and there is no later section left to misalign - so
        // the old bare-character split simply dropped the remainder, and the next save wrote that
        // truncated result over the player's file (issue #38). Two records here and nowhere else,
        // so the only record boundary in the file is the one in section 9.
        var original = SaveFileFixtures.CreateEmpty();
        original.ProceduralInstances.Add(new Models.ProceduralInstance
        {
            Index = 1,
            Position = "1,2,3",
            Rotation = "0,0,0,1",
            WorldObjectIdsGenerated = "",
            WorldObjectIdsDropped = ""
        });
        original.ProceduralInstances.Add(new Models.ProceduralInstance
        {
            Index = 2,
            Position = "4,5,6",
            Rotation = "0,0,0,1",
            WorldObjectIdsGenerated = "",
            WorldObjectIdsDropped = ""
        });
        var content = _serializer.Serialize(original);

        var tampered = content.Replace("|\n{", "|\n@{", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(tampered));
    }

    [Fact]
    public void Deserialize_MoreSectionsThanThisBuildKnows_ThrowsInvalidDataException()
    {
        // An 11th section from a future game version used to be read past and then dropped on
        // the next save (issue #20). Trim the trailing terminator, append a framed extra section.
        var content = _serializer.Serialize(SaveFileFixtures.CreateEmpty());
        var withExtraSection = content[..^2] + "\r@\r{\"future\":1}\r@";

        var exception = Assert.Throws<InvalidDataException>(
            () => _serializer.Deserialize(withExtraSection));

        Assert.Contains("newer version", exception.Message);
    }
}
