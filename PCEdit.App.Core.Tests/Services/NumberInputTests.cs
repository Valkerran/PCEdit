using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class NumberInputTests
{
    // A comma is not a decimal point here and is not a thousands separator either: .NET does not
    // check where a thousands separator sits, so "12,5" used to read as 125 (issue #72).
    [Theory]
    [InlineData("12,5")]
    [InlineData("-120,5")]
    [InlineData("1,234.5")]
    [InlineData("1,234")]
    [InlineData("5-")]
    [InlineData("1.2.3")]
    [InlineData("12 5")]
    [InlineData("0x10")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TextThatIsNotAPlainNumber_IsRefused(string? text)
    {
        Assert.False(NumberInput.TryParseDecimal(text, out _));
        Assert.False(NumberInput.TryParseDouble(text, out _));
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("-120.5", -120.5)]
    [InlineData("+3", 3)]
    [InlineData(" 7 ", 7)]
    [InlineData(".5", 0.5)]
    [InlineData("1.5e2", 150)]
    [InlineData("1E-05", 0.00001)]
    public void PlainNumber_IsRead(string text, double expected)
    {
        Assert.True(NumberInput.TryParseDecimal(text, out var asDecimal));
        Assert.True(NumberInput.TryParseDouble(text, out var asDouble));
        Assert.Equal((decimal)expected, asDecimal);
        Assert.Equal(expected, asDouble);
    }

    // double.TryParse accepts these; none could be written to a save.
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e999")]
    public void NonFiniteDouble_IsRefused(string text)
    {
        Assert.False(NumberInput.TryParseDouble(text, out _));
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData(" 12 ", 12)]
    [InlineData("-5", -5)]
    public void WholeNumber_IsRead(string text, int expected)
    {
        Assert.True(NumberInput.TryParseWholeNumber(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,000")]
    [InlineData("1.5")]
    [InlineData("1e3")]
    [InlineData("5-")]
    [InlineData("99999999999")]
    [InlineData("")]
    [InlineData(null)]
    public void TextThatIsNotAWholeNumber_IsRefused(string? text)
    {
        Assert.False(NumberInput.TryParseWholeNumber(text, out _));
    }
}
