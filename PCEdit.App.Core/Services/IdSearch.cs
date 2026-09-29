using System.Globalization;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The "search by id" rule shared by the Inventories page and the Move dialog, so the two
/// cannot drift apart (issue #3).
/// </summary>
/// <remarks>
/// A search made only of digits - optionally after a leading <c>#</c>, which is how the app
/// displays ids - is an id search, and matches ids by <b>prefix</b>. Measured on a real 2.102
/// save (1063 inventories, 9-digit world-object ids): prefix narrows as the user types ("1014"
/// → 7 inventories) and a short id like "88" finds just inventory 88, where a substring match
/// hit 274 inventories and an exact match showed nothing until the last digit.
///
/// An id search deliberately does <b>not</b> fall back to name matching: container labels
/// embed the container's object id, so a substring hit there brought the noise straight back
/// ("88" → 25 inventories). Only 5 of 628 item names contain a standalone number.
/// </remarks>
public static class IdSearch
{
    /// <summary>
    /// True when <paramref name="term"/> is an id search; <paramref name="digits"/> is then the
    /// prefix to match (without any leading <c>#</c>).
    /// </summary>
    public static bool TryGetIdPrefix(string term, out string digits)
    {
        var trimmed = term.Trim();
        if (trimmed.StartsWith('#'))
        {
            trimmed = trimmed[1..];
        }

        digits = trimmed;
        return trimmed.Length > 0 && trimmed.All(char.IsAsciiDigit);
    }

    /// <summary>True when <paramref name="id"/>, written out, starts with <paramref name="digits"/>.</summary>
    public static bool Matches(int id, string digits) =>
        id.ToString(CultureInfo.InvariantCulture).StartsWith(digits, StringComparison.Ordinal);
}
