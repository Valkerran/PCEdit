using PCEdit.SaveFileHandler;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// <see cref="PlanetHash.Of"/> must reproduce the game's Unity <c>GetStableHashCode</c> exactly —
/// it is the bridge that lets the app match a <c>WorldObject.planet</c> integer back to a string
/// planet id.
/// </summary>
public sealed class PlanetHashTests
{
    private readonly PlanetCrafterSaveFileSerializer _serializer = new(new JsonRecordSerializer());

    [Theory]
    [InlineData("Prime", -1140328421)]
    [InlineData("Selenea", -1016990411)]
    [InlineData("Aqualis", -1291310150)]
    [InlineData("Humble", -486276833)]
    [InlineData("Toxicity", 110910045)]
    public void Of_KnownPlanetIds_MatchTheHashesTheGameWrites(string planetId, int expected)
    {
        Assert.Equal(expected, PlanetHash.Of(planetId));
    }

    // Both fixtures are single-planet saves (2.008 Prime / 2.102 Humble), so every placed world
    // object must carry that planet's hash.
    [Theory]
    [InlineData("Standard-2.json")]
    [InlineData("Humble-2.102.json")]
    public void Of_MatchesThePlanetIntOnARealSaveFilesWorldObjects(string fixtureName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fixtureName);
        var save = _serializer.Deserialize(File.ReadAllText(path));

        var planetHash = PlanetHash.Of(save.Metadata.PlanetId);
        var placed = save.WorldObjects.Where(w => w.Planet is not null).ToList();

        Assert.NotEmpty(placed);
        Assert.All(placed, w => Assert.Equal(planetHash, w.Planet));
    }
}
