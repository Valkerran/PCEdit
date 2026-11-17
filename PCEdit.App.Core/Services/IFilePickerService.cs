namespace PCEdit.App.Core.Services;

/// <summary>
/// What a file picker gave back: a path, nothing (cancelled), or a file PCEdit cannot use because
/// it has no path on this computer - a portal or remote location. That last case used to come
/// back as null and look exactly like Cancel, so the user picked a file and nothing happened (#107).
/// </summary>
public sealed record FilePick(string? Path, bool IsNotLocal)
{
    public static FilePick Cancelled { get; } = new(null, false);

    public static FilePick NotLocal { get; } = new(null, true);

    public static FilePick At(string path) => new(path, false);
}

public interface IFilePickerService
{
    /// <summary>Shows a file picker and returns the chosen file.</summary>
    Task<FilePick> PickSaveFileAsync(string pickerTitle);

    /// <summary>
    /// Shows a "save as" picker that starts at <paramref name="suggestedPath"/> and asks before
    /// overwriting an existing file. Returns the chosen location.
    /// </summary>
    Task<FilePick> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath);
}
