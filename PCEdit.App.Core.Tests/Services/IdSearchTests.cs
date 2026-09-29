using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class IdSearchTests
{
    [Theory]
    [InlineData("88", "88")]
    [InlineData("#88", "88")]
    [InlineData("  #101464942 ", "101464942")]
    public void TryGetIdPrefix_AcceptsDigitsWithAnOptionalHash(string term, string expected)
    {
        Assert.True(IdSearch.TryGetIdPrefix(term, out var digits));
        Assert.Equal(expected, digits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("t2")]
    [InlineData("88a")]
    [InlineData("8 8")]
    [InlineData("-5")]
    [InlineData("٣")] // a non-ASCII digit is not an id
    public void TryGetIdPrefix_RejectsAnythingThatIsNotAnId(string term)
    {
        Assert.False(IdSearch.TryGetIdPrefix(term, out _));
    }

    [Theory]
    [InlineData(88, "8", true)]
    [InlineData(88, "88", true)]
    [InlineData(101464942, "1014", true)]
    [InlineData(188, "88", false)] // prefix, not substring
    [InlineData(8, "88", false)]
    public void Matches_ComparesByPrefix(int id, string digits, bool expected)
    {
        Assert.Equal(expected, IdSearch.Matches(id, digits));
    }
}
