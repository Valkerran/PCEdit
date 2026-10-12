using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace PCEdit.Desktop.Tests;

public sealed class SaveStatusTests
{
    [AvaloniaFact]
    public void ASuccessfulSave_ShowsTheHeaderStatusAsASuccess()
    {
        using var ui = new UiHarness();
        ui.LoadSave();
        ui.MakeAnEdit();

        ui.ClickSave();

        var status = ui.Find<TextBlock>("SaveStatusText");
        Assert.Contains("success", status.Classes);
        Assert.DoesNotContain("error", status.Classes);
    }

    [AvaloniaFact]
    public void AFailedSave_ShowsTheHeaderStatusAsAnError()
    {
        using var ui = new UiHarness();
        var path = ui.LoadSave();
        ui.MakeAnEdit();
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            ui.ClickSave();

            var status = ui.Find<TextBlock>("SaveStatusText");
            Assert.Contains("error", status.Classes);
            Assert.DoesNotContain("success", status.Classes);
            Assert.True(ui.Workspace.IsDirty);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }
}
