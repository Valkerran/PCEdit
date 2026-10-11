using PCEdit.App.Core.Localization;

namespace PCEdit.App.Core.Services;

/// <summary>
/// What the UI head does with an exception nothing else caught: write it to the log and tell the
/// player, instead of letting it end the process with their unsaved edits (issue #74).
/// </summary>
public sealed class UnexpectedErrorReporter(IDialogService dialogs, ILocalizer localizer, string logPath)
{
    private bool _showing;

    public async Task ReportAsync(Exception exception)
    {
        System.Diagnostics.Trace.WriteLine($"{DateTime.UtcNow:O} Unexpected error: {exception}");

        // One dialog at a time: an error raised while this one is open - by showing it, even -
        // is logged above and otherwise left alone, or the dialogs would stack without end.
        if (_showing)
        {
            return;
        }

        _showing = true;
        try
        {
            await dialogs.ShowErrorAsync(
                localizer[LocKeys.Error_UnexpectedTitle],
                localizer.Format(LocKeys.Error_UnexpectedBody, logPath));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Could not show the error dialog: {ex}");
        }
        finally
        {
            _showing = false;
        }
    }
}
