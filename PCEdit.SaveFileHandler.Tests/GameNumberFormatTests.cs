using PCEdit.SaveFileHandler.Models;

namespace PCEdit.SaveFileHandler.Tests;

/// <summary>
/// The game writes every float-valued field with a fractional part; a whole-number edit must
/// re-serialise as <c>N.0</c>, not <c>N</c>, or it shows up as noise in a load→save diff. A number
/// nobody edited must come back with exactly the text it was read with (issue #85).
/// </summary>
public sealed class GameNumberFormatTests
{
    private readonly JsonRecordSerializer _serializer = new();

    [Fact]
    public void WholeNumber_SerializesWithATrailingZero()
    {
        var terraformation = new PlanetTerraformation { PlanetId = "Prime", UnitInsectsLevel = 12345 };

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

    // Every one of these used to change, or (1E+30) refuse to load, because the value went
    // through decimal (issue #85).
    [Theory]
    [InlineData("-0.0")]
    [InlineData("1E-05")]
    [InlineData("1.5e3")]
    [InlineData("1E+30")]
    [InlineData("1234567890123456789.0")]
    [InlineData("1.00000000000000000000000000001")]
    public void UneditedNumber_RoundTripsCharacterForCharacter(string number)
    {
        var json = $$"""{"planetId":"Prime","unitOxygenLevel":{{number}},"unitHeatLevel":0.0,"unitPressureLevel":0.0,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""";

        Assert.Equal(json, _serializer.Serialize(_serializer.Deserialize<PlanetTerraformation>(json, sectionIndex: 1)));
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("null")]
    [InlineData("1e400")]
    public void BadNumber_ErrorNamesTheKey(string value)
    {
        var json = $$"""{"planetId":"Prime","unitOxygenLevel":{{value}},"unitHeatLevel":0.0,"unitPressureLevel":0.0,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""";

        var exception = Assert.Throws<InvalidDataException>(() => _serializer.Deserialize<PlanetTerraformation>(json, sectionIndex: 1, recordIndex: 0));

        Assert.Contains("$.unitOxygenLevel", exception.Message);
    }

    [Theory]
    [InlineData("1e400")]
    [InlineData("\"12\"")]
    public void NonFiniteOrNonNumericValue_IsRejected(string value)
    {
        var json = $$"""{"planetId":"Prime","unitOxygenLevel":{{value}},"unitHeatLevel":0.0,"unitPressureLevel":0.0,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""";

        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize<PlanetTerraformation>(json, sectionIndex: 1));
    }

    [Theory]
    [InlineData(55.90818786621094, "55.90818786621094")]
    [InlineData(38475172675584.0, "38475172675584.0")]
    [InlineData(-0.0, "-0.0")]
    public void EditedNumber_IsWrittenTheWayTheGameWritesOne(double value, string expected)
    {
        var terraformation = new PlanetTerraformation { PlanetId = "Prime", UnitHeatLevel = value };

        Assert.Contains($"\"unitHeatLevel\":{expected}", _serializer.Serialize(terraformation));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteValue_IsRefused(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameNumber(value));
    }

    // The old text must never outlive the old value.
    [Fact]
    public void ReplacedNumber_IsWrittenWithItsOwnText_NotTheOldOne()
    {
        var json = """{"planetId":"Prime","unitOxygenLevel":0.0,"unitHeatLevel":1E-05,"unitPressureLevel":0.0,"unitPlantsLevel":0.0,"unitInsectsLevel":0.0,"unitAnimalsLevel":0.0,"unitPurificationLevel":-1.0}""";
        var read = _serializer.Deserialize<PlanetTerraformation>(json, sectionIndex: 1);

        var written = _serializer.Serialize(read with { UnitHeatLevel = 2.5 });

        Assert.Contains("\"unitHeatLevel\":2.5", written);
        Assert.DoesNotContain("1E-05", written);
    }
}
