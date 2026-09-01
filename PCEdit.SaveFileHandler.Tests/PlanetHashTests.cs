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

    /// <summary>
    /// The interplanetary fixture spans Prime, Aqualis and Selenea, so no single hash covers it --
    /// every placed world object must instead resolve to one of the planets the save knows about.
    /// This is the bridge <c>PlanetIndex.ResolvePlanetId</c> relies on.
    /// </summary>
    [Fact]
    public void Of_ResolvesEveryPlacedWorldObject_OnAMultiPlanetSave()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Interplanetary-2.102.json");
        var save = _serializer.Deserialize(File.ReadAllText(path));

        var known = save.Terraformations.Select(t => PlanetHash.Of(t.PlanetId)).ToHashSet();
        var placed = save.WorldObjects.Where(w => w.Planet is not null).ToList();

        Assert.True(save.Terraformations.Count > 1, "fixture is meant to span several planets");
        Assert.NotEmpty(placed);
        Assert.All(placed, w => Assert.Contains(w.Planet!.Value, known));
    }
}
