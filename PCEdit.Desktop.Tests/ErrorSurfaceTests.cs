using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using PCEdit.App.Core.Localization;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Tests;

public sealed class ErrorSurfaceTests
{
    [AvaloniaFact]
    public void ASettingThatCannotBeSaved_OpensOneDialog()
    {
        using var ui = new UiHarness();
        File.SetAttributes(AppPaths.Settings, FileAttributes.ReadOnly);
        try
        {
            ui.Shell.SelectedLanguage = ui.Shell.Languages.First(language => language.CultureName == "fr");
            ui.Pump();
            var titleWhenItOpened = ui.Localizer[LocKeys.Settings_WriteFailedTitle];
            ui.Shell.SelectedLanguage = ui.Shell.Languages.First(language => language.CultureName == "de");
            ui.Pump();

            var dialog = Assert.Single(ui.Dialogs);
            Assert.Equal(titleWhenItOpened, UiHarness.TitleOf(dialog));
            ui.Click(dialog, 0);
            Assert.Empty(ui.Dialogs);
        }
        finally
        {
            File.SetAttributes(AppPaths.Settings, FileAttributes.Normal);
        }
    }

    [AvaloniaFact]
    public void ASaveThatChangedOnDisk_AsksFirst_AndCancelLeavesEverythingAlone()
    {
        using var ui = new UiHarness();
        var path = ui.LoadSave();
        ui.MakeAnEdit();
        File.AppendAllText(path, " "); // the game saving over it, in miniature
        var onDisk = File.ReadAllBytes(path);

        ui.ClickSave();
        var prompt = Assert.Single(ui.Dialogs);
        Assert.Equal(ui.Localizer[LocKeys.Save_ChangedTitle], UiHarness.TitleOf(prompt));
        ui.Click(prompt, 0); // Cancel is first, the primary button last

        Assert.Equal(onDisk, File.ReadAllBytes(path));
        Assert.True(ui.Workspace.IsDirty);
    }

    [AvaloniaFact]
    public void ASaveThatChangedOnDisk_Overwrite_BacksUpTheNewerFile_ThenWrites()
    {
        using var ui = new UiHarness();
        var path = ui.LoadSave();
        ui.MakeAnEdit();
        File.AppendAllText(path, " ");
        var onDisk = File.ReadAllBytes(path);

        ui.ClickSave();
        ui.Click(Assert.Single(ui.Dialogs), 1);

        Assert.NotEqual(onDisk, File.ReadAllBytes(path));
        Assert.False(ui.Workspace.IsDirty);
        Assert.Equal(onDisk, File.ReadAllBytes(Assert.Single(Directory.GetFiles(AppPaths.Backups))));
    }

    [AvaloniaFact]
    public void TheMoveDialogsStatusLine_IsALiveRegion()
    {
        // A failed move is reported only on this line. The app's announcer speaks through a
        // region in the main window, which a screen reader may not read while this dialog has
        // the focus, so the line has to be able to speak for itself.
        var view = new SelectInventoryView();

        var status = view.FindControl<TextBlock>("StatusText");

        Assert.NotNull(status);
        Assert.Equal(AutomationLiveSetting.Assertive, AutomationProperties.GetLiveSetting(status));
    }
}
