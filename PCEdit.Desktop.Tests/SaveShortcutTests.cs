using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using PCEdit.App.Core.Localization;

namespace PCEdit.Desktop.Tests;

public sealed class SaveShortcutTests
{
    // The headless platform reports Ctrl as the command key; Meta stands in for a Mac's Cmd, which
    // a hard-coded Ctrl+S never answered (#106).
    [AvaloniaTheory]
    [InlineData(KeyModifiers.Meta)]
    [InlineData(KeyModifiers.Control)]
    public void ThePlatformsCommandKeyPlusS_SavesTheFile(KeyModifiers commandKey)
    {
        var hotkeys = Application.Current!.PlatformSettings!.HotkeyConfiguration;
        var original = hotkeys.CommandModifiers;
        hotkeys.CommandModifiers = commandKey;
        try
        {
            using var ui = new UiHarness();
            var path = ui.LoadSave();
            var before = File.ReadAllBytes(path);
            ui.MakeAnEdit();

            ui.Window.KeyPress(Key.S, (RawInputModifiers)commandKey, PhysicalKey.S, "s");
            ui.Window.KeyRelease(Key.S, (RawInputModifiers)commandKey, PhysicalKey.S, "s");
            ui.Pump();

            Assert.NotEqual(before, File.ReadAllBytes(path));
            Assert.Equal(ui.Localizer[LocKeys.Save_Ok], ui.Find<TextBlock>("SaveStatusText").Text);
        }
        finally
        {
            hotkeys.CommandModifiers = original;
        }
    }
}
