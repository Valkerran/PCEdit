using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Tests;

public sealed class ShellSmokeTests
{
    [AvaloniaFact]
    public void TheMainWindowOpens_OnTheOpenFilePage_WithNoDialog()
    {
        using var ui = new UiHarness();

        Assert.Equal(ui.Localizer[LocKeys.Nav_OpenFile], ui.Find<TextBlock>("PageTitle").Text);
        Assert.Empty(ui.Dialogs);
    }

    [AvaloniaFact]
    public void TestsNeverUseTheRealSettingsOrLogFolders()
    {
        using var ui = new UiHarness();

        // The log is opened when the app starts, before any harness exists: this is the one
        // path a test could leak to the real profile without the lines below noticing.
        Assert.StartsWith(TestApp.Root, LogFile.StartedAt);
        Assert.True(File.Exists(LogFile.StartedAt));

        Assert.StartsWith(ui.Root, AppPaths.Settings);
        Assert.StartsWith(ui.Root, AppPaths.Backups);
        Assert.True(File.Exists(AppPaths.Settings)); // the harness's acknowledgement landed here
    }

    [AvaloniaFact]
    public void WithoutAnAcknowledgement_TheDisclaimerOpens_AndAcknowledgingItIsRemembered()
    {
        using var ui = new UiHarness(acknowledgeDisclaimer: false);

        var disclaimer = Assert.Single(ui.Dialogs);
        Assert.Equal(ui.Localizer[LocKeys.Disclaimer_Title], UiHarness.TitleOf(disclaimer));

        ui.Click(disclaimer, 0);

        Assert.Empty(ui.Dialogs);
        Assert.True(ui.Services.GetRequiredService<JsonSettingsStore>().HasAcknowledged);
    }

    [AvaloniaFact]
    public void ClosingTheDisclaimerWithoutItsButton_DoesNotCountAsAcknowledging()
    {
        // The title-bar X or Escape: the user saw it but did not say "I understand", so it shows
        // again next launch. The app stays usable meanwhile (#99).
        using var ui = new UiHarness(acknowledgeDisclaimer: false);

        Assert.Single(ui.Dialogs).Close();
        ui.Pump();

        Assert.Empty(ui.Dialogs);
        Assert.False(ui.Services.GetRequiredService<JsonSettingsStore>().HasAcknowledged);
        Assert.True(ui.Window.IsVisible);
    }

    [AvaloniaFact]
    public void ASecondCloseRequest_WhileTheDiscardPromptIsOpen_DoesNotOpenAnother()
    {
        // Taskbar Close, a repeated Alt+F4 or a session-end request while the prompt is up (#102).
        using var ui = new UiHarness();
        ui.LoadSave();
        ui.MakeAnEdit();

        ui.Window.Close();
        ui.Pump();
        ui.Window.Close();
        ui.Pump();

        // A second prompt would open over the first (dialogs belong to the innermost window), so
        // count every open dialog, not just the main window's own.
        var prompt = Assert.Single(AllDialogs(ui.Window));
        ui.Click(prompt, 0); // Keep editing
        Assert.True(ui.Window.IsVisible);

        ui.Window.Close(); // the guard is cleared: a later close asks again
        ui.Pump();
        Assert.Single(AllDialogs(ui.Window));
    }

    private static IEnumerable<MessageDialog> AllDialogs(Window owner) =>
        owner.OwnedWindows.SelectMany(w => AllDialogs(w).Prepend(w as MessageDialog)).OfType<MessageDialog>();

    [AvaloniaFact]
    public void EditingALoadedSave_EnablesSave_AndSavingWritesTheFile()
    {
        using var ui = new UiHarness();
        var path = ui.LoadSave();
        var before = File.ReadAllBytes(path);
        Assert.False(ui.Find<Button>("SaveButton").IsEffectivelyEnabled);

        ui.MakeAnEdit();
        Assert.True(ui.Find<Button>("SaveButton").IsEffectivelyEnabled);
        ui.ClickSave();

        Assert.NotEqual(before, File.ReadAllBytes(path));
        Assert.Equal(ui.Localizer[LocKeys.Save_Ok], ui.Find<TextBlock>("SaveStatusText").Text);
        Assert.Single(Directory.GetFiles(AppPaths.Backups));
    }

    [AvaloniaFact]
    public void AnUnexpectedError_ShowsOneDialog_AndTheWindowSurvives()
    {
        using var ui = new UiHarness { ExpectUnexpectedError = true };

        Dispatcher.UIThread.Post(() => throw new InvalidOperationException("provoked by the test"));
        ui.Pump();

        var dialog = Assert.Single(ui.Dialogs);
        Assert.Equal(ui.Localizer[LocKeys.Error_UnexpectedTitle], UiHarness.TitleOf(dialog));
        Assert.DoesNotContain("provoked by the test", UiHarness.MessageOf(dialog));
        ui.Click(dialog, 0);
        Assert.True(ui.Window.IsVisible);
    }

    [AvaloniaFact]
    public void AnUnexpectedError_FailsTheTest_UnlessItWasExpected()
    {
        var ui = new UiHarness();
        Dispatcher.UIThread.Post(() => throw new InvalidOperationException("not expected"));
        ui.Pump();

        var failure = Assert.Throws<InvalidOperationException>(ui.Dispose);

        Assert.Equal("not expected", failure.InnerException?.Message);
    }

    // The same check twice, on purpose. A new app object is made for every test, and each used
    // to open the log again under whatever folder the previous test had just deleted. Only a
    // test that is not the first to run can see that, and the order is not ours to choose.
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void TheLogIsOpenedOncePerRun_NotOncePerTest(int _)
    {
        using var ui = new UiHarness();

        Assert.Single(System.Diagnostics.Trace.Listeners.OfType<QuietTraceListener>());
        Assert.Equal(Path.Combine(TestApp.Root, "local", "PCEdit", "pcedit.log"), LogFile.StartedAt);
    }
}
