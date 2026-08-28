using System.Globalization;

namespace PCEdit.App.Core.Services;

public static class PositionCodec
{
    public static (decimal X, decimal Y, decimal Z) Parse(string position)
    {
        var parts = position.Split(',');
        if (parts.Length != 3)
        {
            throw new FormatException($"Expected an \"x,y,z\" position, got \"{position}\".");
        }

        return (
            decimal.Parse(parts[0], CultureInfo.InvariantCulture),
            decimal.Parse(parts[1], CultureInfo.InvariantCulture),
            decimal.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    public static string Format(decimal x, decimal y, decimal z)
    {
        return string.Join(",",
            x.ToString(CultureInfo.InvariantCulture),
            y.ToString(CultureInfo.InvariantCulture),
            z.ToString(CultureInfo.InvariantCulture));
    }
}
