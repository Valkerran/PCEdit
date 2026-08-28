using Avalonia.Threading;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// Avalonia has no first-class "announce" API. This routes messages to a hidden live-region
/// <c>TextBlock</c> that <c>MainWindow</c> registers via <see cref="Sink"/>; assistive tech reads
/// the update (WCAG 2.1 SC 4.1.3). If no sink is registered it is a no-op — the same text is also
/// shown visually on every page, so this is never a correctness issue.
/// </summary>
public sealed class AvaloniaScreenReaderAnnouncer : IScreenReaderAnnouncer
{
    public Action<string>? Sink { get; set; }

    public void Announce(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || Sink is null)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Sink(message);
        }
        else
        {
            Dispatcher.UIThread.Post(() => Sink?.Invoke(message));
        }
    }
}
