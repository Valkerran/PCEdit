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

    [Theory]
    [InlineData("1,not-an-id,3", new[] { 1, 3 })]
    [InlineData("99999999999", new int[0])]          // too large for Int32
    [InlineData("junk", new int[0])]
    public void Parse_SkipsEntriesItCannotRead(string csv, int[] expected)
    {
        // int.Parse threw here, and the Inventories page calls this straight from Load - so one
        // bad entry in a save took the whole app down after the file had opened (issue #37).
        Assert.Equal(expected, WorldObjectIdsCodec.Parse(csv));
    }

    [Fact]
    public void ParseUnreadable_ReturnsTheSkippedEntriesVerbatim()
    {
        Assert.Equal(["not-an-id"], WorldObjectIdsCodec.ParseUnreadable("1,not-an-id,3"));
    }

    [Fact]
    public void ParseUnreadable_WhenEveryEntryIsAnId_ReturnsEmpty()
    {
        Assert.Empty(WorldObjectIdsCodec.ParseUnreadable("1,2,3"));
    }

    [Fact]
    public void Join_WithUnreadableEntries_WritesThemBack()
    {
        // Rewriting the list must not delete bytes PCEdit did not understand.
        Assert.Equal("1,3,not-an-id", WorldObjectIdsCodec.Join([1, 3], ["not-an-id"]));
    }

    [Fact]
    public void ParseThenJoin_PreservesEntriesThatAreNotIds()
    {
        const string csv = "200,junk,201";

        var rejoined = WorldObjectIdsCodec.Join(
            WorldObjectIdsCodec.Parse(csv),
            WorldObjectIdsCodec.ParseUnreadable(csv));

        Assert.Equal("200,201,junk", rejoined);
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
