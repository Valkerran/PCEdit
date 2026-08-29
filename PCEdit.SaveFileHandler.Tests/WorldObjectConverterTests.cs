using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// The game does not write world-object keys in a stable order, so the converter remembers the
/// order each record was read with and replays it — the only way a load→save leaves an untouched
/// record byte-identical.
/// </summary>
public sealed class WorldObjectConverterTests
{
    private readonly JsonRecordSerializer _serializer = new();

    [Theory]
    [InlineData("""{"id":1,"gId":"Container1","liId":5,"liGrps":"Iron","pos":"1,2,3","rot":"0,0,0,1","planet":9,"text":"label"}""")]
    [InlineData("""{"id":2,"gId":"Container1","pos":"1,2,3","rot":"0,0,0,1","planet":9,"liId":5}""")]
    [InlineData("""{"id":3,"gId":"GenerationGroupVein","grwth":99,"count":"124,125"}""")]
    [InlineData("""{"id":4,"gId":"X","pos":"1,2,3","rot":"0,0,0,1","planet":9,"grwth":50}""")]
    [InlineData("""{"id":5,"gId":"X","color":"red","trtInd":3,"trtVal":2}""")]
    [InlineData("""{"id":6,"gId":"X","liId":7,"liGrps":"A","pos":"1,2,3","rot":"0,0,0,1","planet":9,"liPlanet":-42,"grwth":10}""")]
    public void RoundTrip_PreservesKeyOrderCharacterForCharacter(string json)
    {
        Assert.Equal(json, _serializer.Serialize(_serializer.Deserialize<WorldObject>(json, sectionIndex: 3)));
    }

    [Fact]
    public void UnknownKey_KeepsItsOriginalPosition_NotShovedToTheEnd()
    {
        const string json = """{"id":1,"gId":"X","pos":"1,2,3","surpriseKey":42,"planet":9}""";

        var reserialized = _serializer.Serialize(_serializer.Deserialize<WorldObject>(json, sectionIndex: 3));

        Assert.Equal(json, reserialized);
    }

    [Fact]
    public void EditingOneField_LeavesEveryOtherKeyInItsOriginalPosition()
    {
        const string json = """{"id":1,"gId":"Container1","pos":"1,2,3","rot":"0,0,0,1","planet":9,"liId":5}""";
        var edited = _serializer.Deserialize<WorldObject>(json, sectionIndex: 3) with { Position = "9,9,9" };

        Assert.Equal(
            """{"id":1,"gId":"Container1","pos":"9,9,9","rot":"0,0,0,1","planet":9,"liId":5}""",
            _serializer.Serialize(edited));
    }

    [Fact]
    public void ObjectBuiltInCode_SerializesInDeclaredPropertyOrder()
    {
        var worldObject = new WorldObject
        {
            Id = 1,
            GId = "Container1",
            Position = "1,2,3",
            LinkedInventoryId = 5,
            Text = "label"
        };

        Assert.Equal(
            """{"id":1,"gId":"Container1","liId":5,"pos":"1,2,3","text":"label"}""",
            _serializer.Serialize(worldObject));
    }

    [Fact]
    public void ClearingAField_DropsItsKey()
    {
        const string json = """{"id":1,"gId":"X","pos":"1,2,3","text":"gone"}""";
        var edited = _serializer.Deserialize<WorldObject>(json, sectionIndex: 3) with { Text = null };

        Assert.Equal("""{"id":1,"gId":"X","pos":"1,2,3"}""", _serializer.Serialize(edited));
    }
}
