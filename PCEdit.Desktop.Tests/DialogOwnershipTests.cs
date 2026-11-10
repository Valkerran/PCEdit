using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using PCEdit.App.Core.Services;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Tests;

public sealed class DialogOwnershipTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MessagesBelongToTheOpenModal_AndClosingThemLeavesItOpen(bool confirm)
    {
        using var ui = new UiHarness();
        var modal = new RepairWindow();
        var modalClosed = modal.ShowDialog(ui.Window);
        ui.Pump();
        var service = ui.Services.GetRequiredService<IDialogService>();
        var messageClosed = confirm
            ? service.ConfirmAsync("Confirm", "Message", "Accept", "Cancel")
            : service.ShowErrorAsync("Error", "Message");
        ui.Pump();

        try
        {
            var message = Assert.Single(modal.OwnedWindows.OfType<MessageDialog>());
            Assert.DoesNotContain(message, ui.Window.OwnedWindows);
            ui.Click(message, 0);
            await messageClosed;
            Assert.True(modal.IsVisible);
            Assert.False(modalClosed.IsCompleted);
            Assert.Same(modal, ui.Services.GetRequiredService<MainWindowAccessor>().ActiveWindow());
        }
        finally
        {
            foreach (var child in modal.OwnedWindows.ToList())
            {
                child.Close();
            }

            modal.Close();
            await modalClosed;
        }

        Assert.Same(ui.Window, ui.Services.GetRequiredService<MainWindowAccessor>().ActiveWindow());
    }
}
