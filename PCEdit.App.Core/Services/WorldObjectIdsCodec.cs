namespace PCEdit.App.Core.Services;

public static class WorldObjectIdsCodec
{
    public static List<int> Parse(string csv)
    {
        return string.IsNullOrEmpty(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
    }

    public static string Join(IEnumerable<int> ids)
    {
        return string.Join(",", ids);
    }
}
