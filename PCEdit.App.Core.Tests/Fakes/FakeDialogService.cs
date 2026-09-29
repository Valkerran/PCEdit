using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>Records dialogs the shared ViewModels ask for; confirmations answer <see cref="ConfirmResult"/>.</summary>
internal sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; }
    public List<(string Title, string Message)> Errors { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string acceptText, string cancelText) =>
        Task.FromResult(ConfirmResult);

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return Task.CompletedTask;
    }

    public Task ShowDisclaimerAsync(string title, string body, string acknowledgeText) => Task.CompletedTask;
}
