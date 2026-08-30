namespace PCEdit.App.Core.Models;

/// <summary>
/// One entry in the editor's priority dropdown: the raw value that would be written, its named
/// level (null for an out-of-range value the save already held), and the label to show.
/// </summary>
public sealed record LogisticsPriorityChoice(int RawValue, LogisticsPriority? Level, string DisplayName)
{
    public bool IsKnownLevel => Level is not null;
}
