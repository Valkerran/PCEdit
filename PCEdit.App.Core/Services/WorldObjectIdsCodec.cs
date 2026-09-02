using System.Globalization;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The comma-separated world-object id list carried by <c>Inventory.woIds</c>.
/// </summary>
/// <remarks>
/// A save is not guaranteed to hold well-formed ids. A hand-edited file, a third-party tool or a
/// future game version can put something in that list which is not an int, and <c>int.Parse</c>
/// brought the whole app down when one did (issue #37).
///
/// <see cref="Parse"/> therefore skips what it cannot read, so the inventory still renders, and
/// <see cref="ParseUnreadable"/> hands those entries back so a write can put them back untouched.
/// Rewriting the list is not a licence to delete bytes PCEdit did not understand - the same
/// principle as the serializer's <c>[JsonExtensionData]</c> catch-all.
/// </remarks>
public static class WorldObjectIdsCodec
{
    /// <summary>The ids in the list, skipping any entry that is not a valid int.</summary>
    public static List<int> Parse(string? csv)
    {
        var ids = new List<int>();

        foreach (var token in Tokenize(csv))
        {
            if (TryParseId(token, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>The entries <see cref="Parse"/> skipped, verbatim, so a write can restore them.</summary>
    public static List<string> ParseUnreadable(string? csv)
    {
        var unreadable = new List<string>();

        foreach (var token in Tokenize(csv))
        {
            if (!TryParseId(token, out _))
            {
                unreadable.Add(token);
            }
        }

        return unreadable;
    }

    public static string Join(IEnumerable<int> ids)
    {
        return string.Join(",", ids);
    }

    /// <summary>
    /// Joins the ids and re-appends the entries that could not be read. They land at the end
    /// rather than their original position: they already sit outside the format PCEdit
    /// understands, and keeping the bytes matters where keeping their order does not.
    /// </summary>
    public static string Join(IEnumerable<int> ids, IEnumerable<string> unreadable)
    {
        return string.Join(
            ",",
            ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).Concat(unreadable));
    }

    private static bool TryParseId(string token, out int id)
    {
        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
    }

    private static IEnumerable<string> Tokenize(string? csv)
    {
        return string.IsNullOrEmpty(csv) ? [] : csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
