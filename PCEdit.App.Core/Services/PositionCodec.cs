using System.Globalization;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The <c>"x,y,z"</c> string shared by <c>PlayerData.PlayerPosition</c> and
/// <c>WorldObject.Position</c>.
/// </summary>
public static class PositionCodec
{
    /// <summary>Reads a position, throwing when the string does not hold one.</summary>
    /// <remarks>
    /// For a position already known to be well-formed. Anything read straight out of a save file
    /// belongs in <see cref="TryParse"/> instead: a malformed position used to take the whole app
    /// down from the Teleport page's Load, after the file had already opened (issue #37).
    /// </remarks>
    public static (decimal X, decimal Y, decimal Z) Parse(string position)
    {
        return TryParse(position, out var parsed)
            ? parsed
            : throw new FormatException($"Expected an \"x,y,z\" position, got \"{position}\".");
    }

    /// <summary>Reads a position, returning false rather than throwing when it is malformed.</summary>
    public static bool TryParse(string? position, out (decimal X, decimal Y, decimal Z) parsed)
    {
        parsed = default;

        if (position is null)
        {
            return false;
        }

        var parts = position.Split(',');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!TryParseComponent(parts[0], out var x) ||
            !TryParseComponent(parts[1], out var y) ||
            !TryParseComponent(parts[2], out var z))
        {
            return false;
        }

        parsed = (x, y, z);
        return true;
    }

    public static string Format(decimal x, decimal y, decimal z)
    {
        return string.Join(",",
            x.ToString(CultureInfo.InvariantCulture),
            y.ToString(CultureInfo.InvariantCulture),
            z.ToString(CultureInfo.InvariantCulture));
    }

    private static bool TryParseComponent(string value, out decimal component)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out component);
    }
}
