using System.Globalization;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Reads a number the user typed into a box. One rule everywhere: the save's own format -
/// optional sign, digits, at most one dot, optional exponent - in the invariant culture.
/// Deliberately not <see cref="NumberStyles.Number"/>: that allows a thousands separator, and
/// .NET does not check where one sits, so "12,5" read as 125 and a player was teleported ten
/// times too far with a success message (issue #72). A comma is refused, never interpreted:
/// reading it as a decimal point would make "1,234" mean 1.234 to someone who meant a thousand.
/// </summary>
public static class NumberInput
{
    public static bool TryParseDecimal(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>A finite number: NaN, Infinity and an overflowing "1e999" parse as a double but
    /// could not be written to a save.</summary>
    public static bool TryParseDouble(string? text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    public static bool TryParseWholeNumber(string? text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
