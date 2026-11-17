using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using PCEdit.App.Core.Models;
using PCEdit.Desktop.ViewModels;

namespace PCEdit.Desktop.Tests;

public sealed class InventoriesPageTests
{
    [AvaloniaFact]
    public void AReload_KeepsTheWorldShownInTheWorldFilter()
    {
        // The shell reloads the page after every Move or logistics edit. Rebuilding the options
        // empties the picker, so this checks the control, not just the view model (issue #95).
        using var ui = new UiHarness();
        ui.LoadSave("Interplanetary-2.102.json"); // Prime, Aqualis and Selenea
        ui.Shell.NavigateTo(NavDestination.Inventories);
        ui.Pump();
        var picker = ui.Window.GetVisualDescendants().OfType<ComboBox>().Single(c => c.Name == "WorldFilter");
        picker.SelectedItem = picker.Items.OfType<WorldFilterOption>().Single(o => o.PlanetId == "Aqualis");
        ui.Pump();

        ui.Shell.ReloadCurrent();
        ui.Pump();

        Assert.Equal("Aqualis", Assert.IsType<WorldFilterOption>(picker.SelectedItem).PlanetId);
    }
}
