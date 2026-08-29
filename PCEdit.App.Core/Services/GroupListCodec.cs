namespace PCEdit.App.Core.Services;

/// <summary>
/// The comma string shared by <c>Inventory.DemandGroups</c> / <c>SupplyGroups</c> — a plain
/// <c>"A,B,C"</c> list of group ids (no spaces in the save). Parsing trims and drops blanks and
/// duplicates while keeping first-seen order; joining is a bare comma join.
/// </summary>
public static class GroupListCodec
{
    public static List<string> Parse(string? csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var raw in csv.Split(','))
        {
            var id = raw.Trim();
            if (id.Length > 0 && seen.Add(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    public static string Join(IEnumerable<string> groupIds)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return string.Join(
            ',',
            groupIds.Select(id => id.Trim()).Where(id => id.Length > 0 && seen.Add(id)));
    }
}
