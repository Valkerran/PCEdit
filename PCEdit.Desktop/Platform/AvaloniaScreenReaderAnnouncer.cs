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
    // The last message handed to the sink; only touched on the UI thread.
    private string? _last;

    public Action<string>? Sink { get; set; }

    public void Announce(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || Sink is null)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Deliver(message);
        }
        else
        {
            Dispatcher.UIThread.Post(() => Deliver(message));
        }
    }

    /// <summary>
    /// A live region speaks only when its text changes, so the same message twice in a row was
    /// heard once (#103). A repeat is cleared first and set again on the next tick - unless a newer
    /// message has taken its place by then.
    /// </summary>
    private void Deliver(string message)
    {
        var repeat = message == _last;
        _last = message;
        if (!repeat)
        {
            Sink?.Invoke(message);
            return;
        }

        Sink?.Invoke(string.Empty);
        Dispatcher.UIThread.Post(() =>
        {
            if (_last == message)
            {
                Sink?.Invoke(message);
            }
        });
    }
}
