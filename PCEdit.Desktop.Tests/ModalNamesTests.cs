using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.Desktop.Tests;

/// <summary>Every control a screen reader reaches in the dialogs says what it is (#80, #104).</summary>
public sealed class ModalNamesTests
{
    [AvaloniaFact]
    public async Task TheLogisticsEditor_NamesItsAddBoxesAndClearButtons_ForDemandAndSupplyApart()
    {
        using var ui = new UiHarness();
        ui.LoadSave();
        var closed = ui.Services.GetRequiredService<INavigationService>()
            .OpenLogisticsEditorAsync(ui.Workspace.Current!.Inventories.First(i => i.Priority is not null).Id);
        ui.Pump();
        var window = Assert.Single(ui.Window.OwnedWindows);
        var vm = (LogisticsEditorViewModel)window.DataContext!;

        var addBoxes = window.GetVisualDescendants().OfType<AutoCompleteBox>().ToList();
        // The ✕ on the "Everything" chip; the header's "Clear" link runs the same command but is named by its text.
        var buttons = window.GetVisualDescendants().OfType<Button>().Where(b => b.Content as string == "✕").ToList();
        var clearDemand = buttons.Single(b => ReferenceEquals(b.Command, vm.ClearDemandCommand));
        var clearSupply = buttons.Single(b => ReferenceEquals(b.Command, vm.ClearSupplyCommand));

        AssertNamedApart(addBoxes[0], addBoxes[1]);
        AssertNamedApart(clearDemand, clearSupply);

        window.Close();
        await closed;
    }

    [AvaloniaFact]
    public async Task TheMoveDialog_NamesItsSearchBox()
    {
        using var ui = new UiHarness();
        ui.LoadSave();
        var save = ui.Workspace.Current!;
        var item = save.Inventories.SelectMany(i => WorldObjectIdsCodec.Parse(i.WorldObjectIds))
            .First(id => save.WorldObjects.Any(w => w.Id == id));
        var closed = ui.Services.GetRequiredService<INavigationService>().OpenSelectInventoryAsync([item]);
        ui.Pump();
        var window = Assert.Single(ui.Window.OwnedWindows);

        var search = window.GetVisualDescendants().OfType<TextBox>()
            .First(t => t.FindAncestorOfType<AutoCompleteBox>() is null && t.FindAncestorOfType<NumericUpDown>() is null);

        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(search)));

        window.Close();
        await closed;
    }

    private static void AssertNamedApart(Control demand, Control supply)
    {
        var demandName = AutomationProperties.GetName(demand);
        var supplyName = AutomationProperties.GetName(supply);
        Assert.False(string.IsNullOrWhiteSpace(demandName));
        Assert.False(string.IsNullOrWhiteSpace(supplyName));
        Assert.NotEqual(demandName, supplyName);
    }
}
