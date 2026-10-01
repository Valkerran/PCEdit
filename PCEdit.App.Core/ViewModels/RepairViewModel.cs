using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// The over-full container repair (issue #64): previews what trimming every container to
/// <see cref="Multiple"/> times its size would remove, then writes the result to a new save file
/// the player picks. The save that was opened is never written - if the repair turns out to break
/// something, the original is still there.
/// </summary>
public sealed partial class RepairViewModel(
    IOverflowRepair repair,
    ISaveFileWorkspace workspace,
    IFilePickerService filePicker,
    IScreenReaderAnnouncer announcer,
    INavigationService navigation,
    IDialogService dialogs,
    ILocalizer localizer) : ObservableObject
{
    private const int MostTypesShown = 8;
    private const int MaxMultiple = 100;

    private RepairPlan _plan = new(0, 0, []);

    /// <summary>Keep at most this many times each container's size; 1 is what an unmodded game allows.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText), nameof(TopTypes), nameof(CanRepair))]
    [NotifyCanExecuteChangedFor(nameof(RepairCommand))]
    private int _multiple = 1;

    public string PreviewText => _plan.ItemCount == 0
        ? localizer[LocKeys.Repair_Nothing]
        : localizer.Format(LocKeys.Repair_Preview, Number(_plan.ItemCount), Number(_plan.ContainerCount));

    /// <summary>The item types losing the most, as "Name: count" lines.</summary>
    public IReadOnlyList<string> TopTypes => _plan.ByType
        .Take(MostTypesShown)
        .Select(t => localizer.Format(LocKeys.Repair_TypeCount, t.DisplayName, Number(t.Count)))
        .ToList();

    /// <summary>
    /// A failed copy is undone by reopening the original from disk, which would also discard any
    /// other unsaved edit - so the repair waits until there are none.
    /// </summary>
    public bool HasUnsavedChanges => workspace.IsDirty;

    public bool CanRepair => _plan.ItemCount > 0 && !HasUnsavedChanges;

    public void Initialize() => Refresh();

    partial void OnMultipleChanged(int value)
    {
        var kept = Math.Clamp(value, 1, MaxMultiple);
        if (kept != value)
        {
            Multiple = kept;
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        _plan = repair.Plan(Multiple);
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(TopTypes));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanRepair));
        RepairCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRepair))]
    private async Task RepairAsync()
    {
        var original = workspace.FilePath ?? throw new InvalidOperationException("No save file is loaded.");
        var target = await filePicker.PickSaveCopyPathAsync(
            localizer[LocKeys.Repair_SaveTitle],
            SaveCopyName.Suggest(original, File.Exists));
        if (target is null)
        {
            return; // nothing has been touched yet
        }

        var done = repair.Apply(Multiple);
        try
        {
            workspace.SaveCopy(target);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not write the repaired copy '{target}': {ex}");
            await UndoFailedRepairAsync(original);
            return;
        }

        announcer.Announce(localizer.Format(LocKeys.Repair_Done, Number(done.ItemCount), Path.GetFileName(target)));
        await navigation.CloseModalAsync();
    }

    /// <summary>
    /// The repair only exists in memory; reopening the original undoes it, so the player is never
    /// left editing a half-repaired save that a later Save would write over the original.
    /// </summary>
    private async Task UndoFailedRepairAsync(string original)
    {
        workspace.Load(original);
        Refresh();
        var message = localizer[LocKeys.Repair_Failed];
        announcer.Announce(localizer.Format(LocKeys.Announce_ErrorPrefix, message));
        await dialogs.ShowErrorAsync(localizer[LocKeys.Repair_FailedTitle], message);
    }

    [RelayCommand]
    private Task CancelAsync() => navigation.CloseModalAsync();

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
