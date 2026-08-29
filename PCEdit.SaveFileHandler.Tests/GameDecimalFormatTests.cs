using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// The game writes every decimal-valued field with a fractional part; a whole-number edit must
/// re-serialise as <c>N.0</c>, not <c>N</c>, or it shows up as noise in a load→save diff.
/// </summary>
public sealed class GameDecimalFormatTests
{
    private readonly JsonRecordSerializer _serializer = new();

    [Fact]
    public void WholeNumberDecimal_SerializesWithATrailingZero()
    {
        var terraformation = new PlanetTerraformation { PlanetId = "Prime", UnitInsectsLevel = 12345m };

        var json = _serializer.Serialize(terraformation);

        Assert.Contains("\"unitInsectsLevel\":12345.0", json);
        Assert.Contains("\"unitOxygenLevel\":0.0", json);
    }

    [Theory]
    [InlineData("""{"planetId":"Prime","unitOxygenLevel":3015792918528.0,"unitHeatLevel":0.0,"unitPressureLevel":-1.0,"unitPlantsLevel":16235.1005859375,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":8167404.5}""")]
    public void ExistingFractionalValues_RoundTripCharacterForCharacter(string json)
    {
        Assert.Equal(json, _serializer.Serialize(_serializer.Deserialize<PlanetTerraformation>(json, sectionIndex: 1)));
    }
}
