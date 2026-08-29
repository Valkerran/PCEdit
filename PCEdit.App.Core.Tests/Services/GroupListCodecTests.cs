using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class GroupListCodecTests
{
    [Theory]
    [InlineData(null, new string[0])]
    [InlineData("", new string[0])]
    [InlineData("Iron", new[] { "Iron" })]
    [InlineData("Iron,Cobalt,Magnesium", new[] { "Iron", "Cobalt", "Magnesium" })]
    [InlineData(" Iron , Cobalt ", new[] { "Iron", "Cobalt" })]
    [InlineData("Iron,Iron,Cobalt", new[] { "Iron", "Cobalt" })]
    [InlineData("Iron,,Cobalt", new[] { "Iron", "Cobalt" })]
    public void Parse_TrimsDropsBlanksAndDuplicates_KeepingOrder(string? csv, string[] expected)
    {
        Assert.Equal(expected, GroupListCodec.Parse(csv));
    }

    [Fact]
    public void Join_IsABareCommaJoin_WithTheSameCleanup()
    {
        Assert.Equal("Iron,Cobalt", GroupListCodec.Join([" Iron ", "Cobalt", "Iron", ""]));
        Assert.Equal("", GroupListCodec.Join([]));
    }

    [Fact]
    public void ParseThenJoin_OfASaveValue_RoundTrips()
    {
        const string value = "Cobalt,Iron,Magnesium,Silicon,Titanium";

        Assert.Equal(value, GroupListCodec.Join(GroupListCodec.Parse(value)));
    }
}
