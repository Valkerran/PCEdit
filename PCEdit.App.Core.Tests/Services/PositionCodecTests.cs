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

    [Theory]
    [InlineData("1,2")]
    [InlineData("1,2,3,4")]
    [InlineData("")]
    [InlineData("not,a,position")]
    [InlineData(null)]
    public void TryParse_InvalidFormat_ReturnsFalseWithoutThrowing(string? position)
    {
        // The Teleport page reads positions straight out of the save during Load, so a malformed
        // one must not throw - it took the app down after the file had opened (issue #37).
        Assert.False(PositionCodec.TryParse(position, out _));
    }

    [Fact]
    public void TryParse_ValidPosition_ReturnsTrueAndTheValue()
    {
        Assert.True(PositionCodec.TryParse("1.5,-2.25,3", out var parsed));

        Assert.Equal((1.5m, -2.25m, 3m), parsed);
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
