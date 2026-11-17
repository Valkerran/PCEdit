using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.Desktop.Tests;

/// <summary>Escape dismisses the Move, Logistics and Repair dialogs as their Cancel does (#105).</summary>
public sealed class ModalKeyboardTests
{
    public enum Modal { Move, Logistics, Repair }

    [AvaloniaTheory]
    [InlineData(Modal.Move)]
    [InlineData(Modal.Logistics)]
    [InlineData(Modal.Repair)]
    public async Task Escape_ClosesTheDialog_WithoutChangingTheSave(Modal modal)
    {
        using var ui = new UiHarness();
        ui.LoadSave();
        var closed = Open(ui, modal);
        var window = Assert.Single(ui.Window.OwnedWindows);

        PressEscape(ui, window);

        Assert.True(closed.IsCompleted);
        await closed;
        Assert.False(ui.Workspace.IsDirty);
    }

    [AvaloniaFact]
    public async Task Escape_WithASuggestionListOpen_ClosesOnlyTheList()
    {
        using var ui = new UiHarness();
        ui.LoadSave();
        var closed = Open(ui, Modal.Logistics);
        var window = Assert.Single(ui.Window.OwnedWindows);
        var box = window.GetVisualDescendants().OfType<AutoCompleteBox>().First();
        box.Focus();
        window.KeyTextInput("Ir"); // typed, as a user opens it: an empty box closes a forced-open list
        ui.Pump();
        Assert.True(box.IsDropDownOpen);

        PressEscape(ui, window);

        Assert.False(box.IsDropDownOpen);
        Assert.False(closed.IsCompleted);

        PressEscape(ui, window);
        Assert.True(closed.IsCompleted);
        await closed;
    }

    private static Task Open(UiHarness ui, Modal modal)
    {
        var navigation = ui.Services.GetRequiredService<INavigationService>();
        var save = ui.Workspace.Current!;
        var closed = modal switch
        {
            Modal.Move => navigation.OpenSelectInventoryAsync([FirstHeldItem(save)]),
            Modal.Logistics => navigation.OpenLogisticsEditorAsync(save.Inventories.First(i => i.Priority is not null).Id),
            _ => navigation.OpenRepairAsync(),
        };
        ui.Pump();
        return closed;
    }

    private static int FirstHeldItem(PlanetCrafterSaveFile save)
    {
        var worldObjectIds = save.WorldObjects.Select(w => w.Id).ToHashSet();
        return save.Inventories.SelectMany(i => WorldObjectIdsCodec.Parse(i.WorldObjectIds)).First(worldObjectIds.Contains);
    }

    private static void PressEscape(UiHarness ui, Window window)
    {
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        if (window.IsVisible) // the press may already have closed it, and a closed window takes no input
        {
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        }

        ui.Pump();
    }
}
