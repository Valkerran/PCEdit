using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class UnexpectedErrorReporterTests
{
    private const string LogPath = "/somewhere/pcedit.log";

    /// <summary>An error dialog that stays open until the test closes it.</summary>
    private sealed class OpenDialogs : IDialogService
    {
        public List<(string Title, string Message)> Shown { get; } = [];

        public TaskCompletionSource Closed { get; } = new();

        public Exception? Failure { get; set; }

        public Task ShowErrorAsync(string title, string message)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Shown.Add((title, message));
            return Closed.Task;
        }

        public Task<bool> ConfirmAsync(string title, string message, string acceptText, string cancelText) =>
            Task.FromResult(false);

        public Task ShowDisclaimerAsync(string title, string body, string acknowledgeText) => Task.CompletedTask;
    }

    [Fact]
    public async Task AnError_IsShown_WithWhereTheDetailsWent()
    {
        var dialogs = new OpenDialogs();
        dialogs.Closed.SetResult();

        await new UnexpectedErrorReporter(dialogs, new Localizer(), LogPath).ReportAsync(new InvalidOperationException("boom"));

        var shown = Assert.Single(dialogs.Shown);
        Assert.Equal(new Localizer()[LocKeys.Error_UnexpectedTitle], shown.Title);
        Assert.Contains(LogPath, shown.Message);
        Assert.DoesNotContain("boom", shown.Message); // details go to the log, not the dialog
    }

    [Fact]
    public async Task ASecondError_WhileTheDialogIsOpen_IsLoggedButNotShown()
    {
        // An error raised by showing the first dialog would otherwise stack dialogs without end.
        var dialogs = new OpenDialogs();
        var reporter = new UnexpectedErrorReporter(dialogs, new Localizer(), LogPath);

        var first = reporter.ReportAsync(new InvalidOperationException("first"));
        await reporter.ReportAsync(new InvalidOperationException("second"));

        Assert.Single(dialogs.Shown);

        dialogs.Closed.SetResult();
        await first;
        await reporter.ReportAsync(new InvalidOperationException("third"));
        Assert.Equal(2, dialogs.Shown.Count);
    }

    [Fact]
    public async Task ADialogThatCannotBeShown_DoesNotThrow()
    {
        var dialogs = new OpenDialogs { Failure = new InvalidOperationException("no window") };

        await new UnexpectedErrorReporter(dialogs, new Localizer(), LogPath).ReportAsync(new InvalidOperationException("boom"));

        Assert.Empty(dialogs.Shown);
    }
}
