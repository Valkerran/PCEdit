namespace PCEdit.App.Core.Services;

public interface IFilePickerService
{
    /// <summary>Shows a file picker and returns the chosen file's full path, or <c>null</c> if cancelled.</summary>
    Task<string?> PickSaveFileAsync(string pickerTitle);
}
