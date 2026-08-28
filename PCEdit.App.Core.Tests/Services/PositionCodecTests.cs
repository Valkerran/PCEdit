using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class PositionCodecTests
{
    [Fact]
    public void Parse_ValidPosition_ReturnsXyzTuple()
    {
        var (x, y, z) = PositionCodec.Parse("1.5,-2.25,3");

        Assert.Equal(1.5m, x);
        Assert.Equal(-2.25m, y);
        Assert.Equal(3m, z);
    }

    [Theory]
    [InlineData("1,2")]
    [InlineData("1,2,3,4")]
    [InlineData("")]
    [InlineData("not,a,position")]
    public void Parse_InvalidFormat_ThrowsFormatException(string position)
    {
        Assert.Throws<FormatException>(() => PositionCodec.Parse(position));
    }

    [Fact]
    public void Format_ProducesCommaSeparatedInvariantCultureString()
    {
        var result = PositionCodec.Format(1.5m, -2.25m, 3m);

        Assert.Equal("1.5,-2.25,3", result);
    }

    [Fact]
    public void FormatThenParse_RoundTrips()
    {
        var formatted = PositionCodec.Format(598.74m, 1.32m, 682.76m);
        var (x, y, z) = PositionCodec.Parse(formatted);

        Assert.Equal(598.74m, x);
        Assert.Equal(1.32m, y);
        Assert.Equal(682.76m, z);
    }
}
