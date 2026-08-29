using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// Pins the model property ↔ JSON key mapping against the abbreviated key spellings the game
/// actually writes. A model-to-model round trip can't catch a wrong <c>[JsonPropertyName]</c>
/// (it reads back whatever it wrote); these tests start from a literal game-format string.
/// </summary>
public sealed class GameFormatKeyMappingTests
{
    private readonly JsonRecordSerializer _serializer = new();

    [Fact]
    public void Inventory_LogisticsKeys_AreDemandGrpsAndSupplyGrps()
    {
        const string json =
            """{"id":372,"woIds":"1,2,3","size":35,"demandGrps":"Magnesium","supplyGrps":"Iron,Cobalt","priority":1}""";

        var inventory = _serializer.Deserialize<Inventory>(json, sectionIndex: 4);

        Assert.Equal("Magnesium", inventory.DemandGroups);
        Assert.Equal("Iron,Cobalt", inventory.SupplyGroups);
        Assert.Equal(1, inventory.Priority);

        var reserialized = _serializer.Serialize(inventory);
        Assert.Contains("\"demandGrps\":\"Magnesium\"", reserialized);
        Assert.Contains("\"supplyGrps\":\"Iron,Cobalt\"", reserialized);
        Assert.DoesNotContain("demandGroups", reserialized);
        Assert.DoesNotContain("supplyGroups", reserialized);
    }

    [Fact]
    public void WorldObject_VeinCollectorAndLabelKeys_MapToTheirProperties()
    {
        const string vein = """{"id":106942627,"gId":"GenerationGroupVein","grwth":99,"count":"124,125"}""";
        const string collector = """{"id":208516079,"gId":"ToxicWaterCollector1","liId":116,"linkedWo":101183694}""";
        const string labelled = """{"id":203021211,"gId":"Container1","liId":43,"text":"w"}""";

        var veinObject = _serializer.Deserialize<WorldObject>(vein, sectionIndex: 3);
        var collectorObject = _serializer.Deserialize<WorldObject>(collector, sectionIndex: 3);
        var labelledObject = _serializer.Deserialize<WorldObject>(labelled, sectionIndex: 3);

        Assert.Equal("124,125", veinObject.MineableCount);
        Assert.Equal(101183694, collectorObject.LinkedWorldObjectId);
        Assert.Equal("w", labelledObject.Text);

        Assert.Contains("\"count\":\"124,125\"", _serializer.Serialize(veinObject));
        Assert.Contains("\"linkedWo\":101183694", _serializer.Serialize(collectorObject));
        Assert.Contains("\"text\":\"w\"", _serializer.Serialize(labelledObject));
    }

    [Fact]
    public void UnknownKey_IsPreservedThroughExtensionData()
    {
        const string json =
            """{"id":1,"woIds":"","size":8,"somethingTheGameAddedLater":"keep me","priority":0}""";

        var inventory = _serializer.Deserialize<Inventory>(json, sectionIndex: 4);

        Assert.NotNull(inventory.ExtensionData);
        Assert.True(inventory.ExtensionData!.ContainsKey("somethingTheGameAddedLater"));

        var reserialized = _serializer.Serialize(inventory);
        Assert.Contains("\"somethingTheGameAddedLater\":\"keep me\"", reserialized);
    }
}
