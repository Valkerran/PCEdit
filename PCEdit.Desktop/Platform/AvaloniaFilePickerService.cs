using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

    public async Task<string?> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath)
    {
        var storage = ActiveWindow().StorageProvider;
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

    /// <summary>
    /// The window the picker belongs to: the one the user is in - a modal such as the repair
    /// dialog, when one is open - so the picker opens on top of it rather than behind it.
    /// </summary>
    private Window ActiveWindow()
    {
        var windows = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows;
        return windows?.FirstOrDefault(w => w.IsActive) ?? _mainWindow.Require();
    }
}
