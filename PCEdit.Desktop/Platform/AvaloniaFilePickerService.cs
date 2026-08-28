using Avalonia.Platform.Storage;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaFilePickerService(MainWindowAccessor mainWindow) : IFilePickerService
{
    private readonly MainWindowAccessor _mainWindow = mainWindow;

    public async Task<string?> PickSaveFileAsync(string pickerTitle)
    {
        var storage = _mainWindow.Require().StorageProvider;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = pickerTitle,
            AllowMultiple = false,
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
}
