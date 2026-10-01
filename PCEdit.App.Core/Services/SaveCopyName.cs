using System.Globalization;
using System.Text.RegularExpressions;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Suggests a file name for a repaired copy of a save, beside the original (issue #64). The game
/// lists its saves as "&lt;Mode&gt;-&lt;N&gt;.json" slots, so a numbered save gets the next free slot of
/// the same mode and shows up in the game's load list; anything else gets a "-repaired" suffix.
/// Only a suggestion - the player confirms or changes it in the save dialog.
/// </summary>
public static partial class SaveCopyName
{
    public static string Suggest(string originalPath, Func<string, bool> exists)
    {
        var folder = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var slot = NumberedSlot().Match(name);
        if (slot.Success && int.TryParse(slot.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            return FirstFree(number => Path.Combine(folder, $"{slot.Groups["mode"].Value}-{number}{extension}"), n + 1, exists);
        }

        var repaired = Path.Combine(folder, $"{name}-repaired{extension}");
        return exists(repaired)
            ? FirstFree(number => Path.Combine(folder, $"{name}-repaired-{number}{extension}"), 2, exists)
            : repaired;
    }

    private static string FirstFree(Func<int, string> candidate, int from, Func<string, bool> exists)
    {
        for (var number = from; ; number++)
        {
            if (!exists(candidate(number)))
            {
                return candidate(number);
            }
        }
    }

    [GeneratedRegex(@"^(?<mode>.+)-(?<n>\d{1,6})$")]
    private static partial Regex NumberedSlot();
}
