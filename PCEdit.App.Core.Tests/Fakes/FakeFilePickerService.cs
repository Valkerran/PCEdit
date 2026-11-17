using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>Answers pickers with preset paths and records what each was asked.</summary>
internal sealed class FakeFilePickerService : IFilePickerService
{
    /// <summary>What a save-copy picker returns; null stands in for the user cancelling.</summary>
    public string? SaveCopyResult { get; set; }

    /// <summary>The save-copy picker returns a location that is not on this computer.</summary>
    public bool SaveCopyNotLocal { get; set; }

    public List<string> SuggestedPaths { get; } = [];

    /// <summary>What the open-file picker returns; null stands in for the user cancelling.</summary>
    public string? PickResult { get; set; }

    /// <summary>The open-file picker returns a file that is not on this computer.</summary>
    public bool PickNotLocal { get; set; }

    public Task<FilePick> PickSaveFileAsync(string pickerTitle) =>
        Task.FromResult(Answer(PickResult, PickNotLocal));

    public Task<FilePick> PickSaveCopyPathAsync(string pickerTitle, string suggestedPath)
    {
        SuggestedPaths.Add(suggestedPath);
        return Task.FromResult(Answer(SaveCopyResult, SaveCopyNotLocal));
    }

    private static FilePick Answer(string? path, bool notLocal) =>
        notLocal ? FilePick.NotLocal : path is null ? FilePick.Cancelled : FilePick.At(path);
}
