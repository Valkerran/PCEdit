namespace PCEdit.App.Core.Services;

public interface IFilePickerService
{
    /// <summary>Shows a file picker and returns the chosen file's full path, or <c>null</c> if cancelled.</summary>
    Task<string?> PickSaveFileAsync(string pickerTitle);

    /// <summary>
    /// Shows a "save as" picker that starts at <paramref name="suggestedPath"/> and asks before
    /// overwriting an existing file. Returns the chosen full path, or <c>null</c> if cancelled.
    /// </summary>
    Task<string?> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath);
}
