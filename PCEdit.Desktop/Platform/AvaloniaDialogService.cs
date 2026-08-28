using Avalonia.Threading;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.Desktop.Views;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaDialogService(MainWindowAccessor mainWindow, ILocalizer localizer) : IDialogService
{
    private readonly MainWindowAccessor _mainWindow = mainWindow;
    private readonly ILocalizer _localizer = localizer;

    public Task<bool> ConfirmAsync(string title, string message, string acceptText, string cancelText) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Buttons are laid out left-to-right; the last one is primary. Cancel first, Accept last.
            var dialog = new MessageDialog(title, message, [cancelText, acceptText]);
            var clicked = await dialog.ShowAsync(_mainWindow.Require());
            return clicked == 1;
        });

    public Task ShowErrorAsync(string title, string message) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new MessageDialog(title, message, [_localizer[LocKeys.Common_Ok]]);
            await dialog.ShowAsync(_mainWindow.Require());
        });

    public Task ShowDisclaimerAsync(string title, string body, string acknowledgeText) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new MessageDialog(title, body, [acknowledgeText]);
            await dialog.ShowAsync(_mainWindow.Require());
        });
}
