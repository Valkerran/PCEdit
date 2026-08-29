namespace PCEdit.App.Core.Models;

/// <summary>
/// One entry in the logistics-group pick-list for a container's demand / supply lists.
/// <see cref="Id"/> is the raw group id stored in the save; <see cref="DisplayName"/> is the
/// friendly label (the raw id again for a group the bundled dataset doesn't know).
/// <see cref="IsKnown"/> is false for such an id — the UI marks it as free-text the user typed.
/// </summary>
public sealed record LogisticsGroupInfo(string Id, string DisplayName, bool IsKnown);
