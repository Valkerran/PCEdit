using Avalonia.Platform.Storage;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaFilePickerService(MainWindowAccessor mainWindow) : IFilePickerService
{
    private readonly MainWindowAccessor _mainWindow = mainWindow;

    public async Task<FilePick> PickSaveFileAsync(string pickerTitle)
    {
        var storage = _mainWindow.ActiveWindow().StorageProvider;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = pickerTitle,
            AllowMultiple = false,
        });

        try
        {
            return files.Count > 0 ? ToFilePick(files[0]) : FilePick.Cancelled;
        }
        finally
        {
            foreach (var file in files)
            {
                file.Dispose();
            }
        }
    }

    public async Task<FilePick> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath)
    {
        var storage = _mainWindow.ActiveWindow().StorageProvider;
        var folder = Path.GetDirectoryName(suggestedPath);
        using var startFolder = string.IsNullOrEmpty(folder) ? null : await storage.TryGetFolderFromPathAsync(folder);

        using var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = pickerTitle,
            SuggestedFileName = Path.GetFileName(suggestedPath),
            SuggestedStartLocation = startFolder,
            DefaultExtension = Path.GetExtension(suggestedPath).TrimStart('.'),
            ShowOverwritePrompt = true,
        });

        return file is null ? FilePick.Cancelled : ToFilePick(file);
    }

    /// <summary>PCEdit reads and writes saves by path, so a pick with none - a portal or remote
    /// location - is reported as such rather than passed off as Cancel (#107).</summary>
    private static FilePick ToFilePick(IStorageItem item) =>
        item.TryGetLocalPath() is { } path ? FilePick.At(path) : FilePick.NotLocal;
}
