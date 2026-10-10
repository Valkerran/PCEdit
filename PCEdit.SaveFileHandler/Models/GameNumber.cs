using System.Globalization;

namespace PCEdit.SaveFileHandler.Models;

/// <summary>
/// A number the game writes as a float - terraform levels, vital gauges, world modifiers. Held as
/// the double the game itself uses, plus the exact text it was read with, so a value nobody
/// edited is written back byte-for-byte: going through decimal turned -0.0 into 0.0 and 1E-05
/// into 0.00001, and refused 1E+30 outright (issue #85). A value set in code has no text and is
/// written the way the game writes one: shortest round-trip, always with a fractional part.
/// Equality compares the value only.
/// </summary>
public readonly record struct GameNumber
{
    /// <exception cref="ArgumentOutOfRangeException">The value is NaN or infinite; it could not be written.</exception>
    public GameNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A game number must be finite.");
        }

        Value = value;
    }

    public double Value { get; }

    internal string? RawText { get; init; }

    public static implicit operator GameNumber(double value) => new(value);

    public bool Equals(GameNumber other) => Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => RawText ?? Format(Value);

    private static string Format(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.Contains('.') || text.Contains('E') ? text : text + ".0";
    }
}
