using CommunityToolkit.Mvvm.ComponentModel;

namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// One item type over capacity in the repair dialog's "move into free storage first" list: tick it
/// to move it before anything is removed (issue #64, phase 6b).
/// </summary>
public sealed partial class RepairTypeChoice(string gId, string displayName) : ObservableObject
{
    public string GId { get; } = gId;

    public string DisplayName { get; } = displayName;

    /// <summary>"Name: count", how many of this type are over capacity at the current setting.</summary>
    [ObservableProperty]
    private string _label = displayName;

    [ObservableProperty]
    private bool _isChecked;
}
