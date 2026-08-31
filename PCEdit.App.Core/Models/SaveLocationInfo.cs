namespace PCEdit.App.Core.Models;

/// <summary>One row in the About page's "where the game keeps its saves" card. Both fields are
/// literal (platform names and filesystem paths don't localize).</summary>
public sealed record SaveLocationInfo(string Platform, string Path);
