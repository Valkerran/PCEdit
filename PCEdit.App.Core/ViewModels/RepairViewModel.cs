using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// The over-full container repair (issue #64): previews what trimming every container to
/// <see cref="Multiple"/> times its size would remove - after moving the item types the player
/// ticks into free storage - then writes the result to a new save file the player picks. The save that was opened is never written - if the repair turns out to break
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
    private const int MaxMultiple = 100;

    private RepairPlan _plan = new(0, 0, []);

    // The player's priority and ticks, by item type. They outlive the rows: a type can drop out
    // of the list at one multiple and come back at another, and should come back as it was left.
    private readonly List<string> _order = [];
    private readonly HashSet<string> _ticked = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RepairTypeChoice> _choices = new(StringComparer.Ordinal);

    /// <summary>Keep at most this many times each container's size; 1 is what an unmodded game allows.</summary>
    [ObservableProperty]
    private int _multiple = 1;

    /// <summary>Each item type over capacity, in the player's priority order, to tick for moving
    /// into free storage first (issue #64, phase 6b).</summary>
    public ObservableCollection<RepairTypeChoice> PriorityTypes { get; } = [];

    public string PreviewText => _plan.ContainerCount == 0
        ? localizer[LocKeys.Repair_Nothing]
        : localizer.Format(LocKeys.Repair_Preview, Number(_plan.ItemCount), Number(_plan.ContainerCount));

    /// <summary>What the ticked types gain: null when nothing moves.</summary>
    public string? MovedText => _plan.MovedCount == 0
        ? null
        : localizer.Format(LocKeys.Repair_PreviewMoved, Number(_plan.MovedCount), Number(_plan.DestinationCount));

    public string FreeSlotsText => localizer.Format(LocKeys.Repair_FreeSlots, Number(_plan.FreeSlots));

    /// <summary>
    /// A failed copy is undone by reopening the original from disk, which would also discard any
    /// other unsaved edit - so the repair waits until there are none.
    /// </summary>
    public bool HasUnsavedChanges => workspace.IsDirty;

    /// <summary>Anything over capacity - removed or moved - is work for the repair.</summary>
    public bool CanRepair => _plan.ContainerCount > 0 && !HasUnsavedChanges;

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

    [RelayCommand]
    private void MoveUp(RepairTypeChoice choice) => Swap(choice, -1);

    [RelayCommand]
    private void MoveDown(RepairTypeChoice choice) => Swap(choice, +1);

    /// <summary>Swaps a row with its neighbour, in the list and in the remembered priority.</summary>
    private void Swap(RepairTypeChoice choice, int step)
    {
        var from = PriorityTypes.IndexOf(choice);
        var to = from + step;
        if (from < 0 || to < 0 || to >= PriorityTypes.Count)
        {
            return;
        }

        var a = _order.IndexOf(choice.GId);
        var b = _order.IndexOf(PriorityTypes[to].GId);
        (_order[a], _order[b]) = (_order[b], _order[a]);
        PriorityTypes.Move(from, to);
        Refresh();
    }

    private IReadOnlyList<string> TickedInOrder() => _order.Where(_ticked.Contains).ToList();

    private void Refresh()
    {
        _plan = repair.Plan(Multiple, TickedInOrder());
        SyncChoices();
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(MovedText));
        OnPropertyChanged(nameof(FreeSlotsText));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(CanRepair));
        RepairCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Brings the rows in line with the plan's excess types, reusing each type's row so
    /// ticking or reordering never rebuilds the list under the player's keyboard focus.</summary>
    private void SyncChoices()
    {
        foreach (var type in _plan.ExcessByType.Where(t => !_order.Contains(t.GId)))
        {
            _order.Add(type.GId); // a newcomer joins the end, most numerous first
        }

        var wanted = _plan.ExcessByType.OrderBy(t => _order.IndexOf(t.GId)).ToList();
        foreach (var type in wanted)
        {
            ChoiceFor(type.GId, type.DisplayName).Label =
                localizer.Format(LocKeys.Repair_TypeCount, type.DisplayName, Number(type.Count));
        }

        if (!PriorityTypes.Select(c => c.GId).SequenceEqual(wanted.Select(t => t.GId)))
        {
            PriorityTypes.Clear();
            foreach (var type in wanted)
            {
                PriorityTypes.Add(_choices[type.GId]);
            }
        }
    }

    private RepairTypeChoice ChoiceFor(string gId, string displayName)
    {
        if (_choices.TryGetValue(gId, out var existing))
        {
            return existing;
        }

        var choice = new RepairTypeChoice(gId, displayName) { IsChecked = _ticked.Contains(gId) };
        choice.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RepairTypeChoice.IsChecked))
            {
                OnChoiceToggled(choice);
            }
        };
        _choices[gId] = choice;
        return choice;
    }

    private void OnChoiceToggled(RepairTypeChoice choice)
    {
        if (choice.IsChecked)
        {
            _ticked.Add(choice.GId);
        }
        else
        {
            _ticked.Remove(choice.GId);
        }

        Refresh();
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

        var done = repair.Apply(Multiple, TickedInOrder());
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

        announcer.Announce(DoneMessage(done, target));
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

    /// <summary>What the repair did, for the screen reader: what moved, if anything, then what
    /// was removed and where the copy was saved.</summary>
    private string DoneMessage(RepairPlan done, string target)
    {
        var summary = localizer.Format(LocKeys.Repair_Done, Number(done.ItemCount), Path.GetFileName(target));
        return done.MovedCount == 0
            ? summary
            : $"{localizer.Format(LocKeys.Repair_DoneMoved, Number(done.MovedCount))} {summary}";
    }

    [RelayCommand]
    private Task CancelAsync() => navigation.CloseModalAsync();

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
