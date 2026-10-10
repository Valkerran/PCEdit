using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>Answers pickers with preset paths and records what each was asked.</summary>
internal sealed class FakeFilePickerService : IFilePickerService
{
    /// <summary>What a save-copy picker returns; null stands in for the user cancelling.</summary>
    public string? SaveCopyResult { get; set; }

    public List<string> SuggestedPaths { get; } = [];

    /// <summary>What the open-file picker returns; null stands in for the user cancelling.</summary>
    public string? PickResult { get; set; }

    public Task<string?> PickSaveFileAsync(string pickerTitle) => Task.FromResult(PickResult);

    public Task<string?> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath)
    {
        SuggestedPaths.Add(suggestedPath);
        return Task.FromResult(SaveCopyResult);
    }
}
