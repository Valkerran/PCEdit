namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// Classifies an inline status message so it can be conveyed by more than colour:
/// the message text is prefixed when spoken and the label colour is chosen from a
/// palette that clears AA contrast in both themes.
/// </summary>
public enum StatusKind
{
    Info,
    Success,
    Error
}
