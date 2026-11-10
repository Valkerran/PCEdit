using Avalonia.Platform.Storage;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaFilePickerService(MainWindowAccessor mainWindow) : IFilePickerService
{
    private readonly MainWindowAccessor _mainWindow = mainWindow;

    public async Task<string?> PickSaveFileAsync(string pickerTitle)
    {
        var storage = _mainWindow.ActiveWindow().StorageProvider;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = pickerTitle,
            AllowMultiple = false,
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath)
    {
        var storage = _mainWindow.ActiveWindow().StorageProvider;
        var folder = Path.GetDirectoryName(suggestedPath);

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = pickerTitle,
            SuggestedFileName = Path.GetFileName(suggestedPath),
            SuggestedStartLocation = string.IsNullOrEmpty(folder) ? null : await storage.TryGetFolderFromPathAsync(folder),
            DefaultExtension = Path.GetExtension(suggestedPath).TrimStart('.'),
            ShowOverwritePrompt = true,
        });

        return file?.TryGetLocalPath();
    }

}
