namespace PCEdit.App.Core.Services;

/// <summary>
/// UI-framework-agnostic modal dialogs used by the shared ViewModels and shell.
/// The UI head (Avalonia) supplies the implementation.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a two-button confirmation. Returns <c>true</c> when the user picks the
    /// accept button, <c>false</c> otherwise (including dismissal).
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string acceptText, string cancelText);

    /// <summary>Shows a single-button error alert.</summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows the blocking "use at your own risk" disclaimer with a single acknowledge button.
    /// Returns <c>true</c> when the user clicks it, <c>false</c> when the dialog was closed some
    /// other way (the title-bar X, Escape) - which is not an acknowledgement (#99).
    /// </summary>
    Task<bool> ShowDisclaimerAsync(string title, string body, string acknowledgeText);
}
