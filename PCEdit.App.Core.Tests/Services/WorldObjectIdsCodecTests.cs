using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class WorldObjectIdsCodecTests
{
    [Fact]
    public void Parse_Null_ReturnsEmptyList()
    {
        Assert.Empty(WorldObjectIdsCodec.Parse(null!));
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyList()
    {
        Assert.Empty(WorldObjectIdsCodec.Parse(""));
    }

    [Fact]
    public void Parse_CommaSeparatedIds_ReturnsParsedInts()
    {
        var result = WorldObjectIdsCodec.Parse("1,2,3");

        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void Parse_SingleId_ReturnsSingleEntry()
    {
        var result = WorldObjectIdsCodec.Parse("42");

        Assert.Equal([42], result);
    }

    [Fact]
    public void Join_EmptyCollection_ReturnsEmptyString()
    {
        Assert.Equal("", WorldObjectIdsCodec.Join([]));
    }

    [Fact]
    public void Join_MultipleIds_ReturnsCommaSeparatedString()
    {
        Assert.Equal("1,2,3", WorldObjectIdsCodec.Join([1, 2, 3]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("5")]
    [InlineData("5,10,15")]
    public void JoinThenParse_RoundTrips(string csv)
    {
        var parsed = WorldObjectIdsCodec.Parse(csv);
        var rejoined = WorldObjectIdsCodec.Join(parsed);

        Assert.Equal(csv, rejoined);
    }
}
