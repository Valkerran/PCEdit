using System.Diagnostics;

namespace PCEdit.App.Core.Services;

/// <summary>
/// A trace listener for the log file that never throws into its caller and stops at a size limit.
/// </summary>
/// <remarks>
/// Diagnostics are mostly written from inside catch blocks, on the way to telling the player what
/// went wrong. A log on a full or vanished disk must not turn "Save failed" into an exception
/// with no message at all.
/// </remarks>
public sealed class QuietTraceListener(TextWriter log, long maxChars) : TraceListener
{
    private long _written;

    public override void Write(string? message) => Append(message, endLine: false);

    public override void WriteLine(string? message) => Append(message, endLine: true);

    public override void Flush() => Quietly(log.Flush);

    private void Append(string? message, bool endLine)
    {
        // The app keeps running after an unexpected error, so one that repeats must not be able
        // to fill the disk with its own report.
        if (_written >= maxChars)
        {
            return;
        }

        _written += (message?.Length ?? 0) + (endLine ? Environment.NewLine.Length : 0);
        Quietly(() =>
        {
            log.Write(message);
            if (endLine)
            {
                log.WriteLine();
            }

            log.Flush();
        });
    }

    private static void Quietly(Action write)
    {
        try
        {
            write();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            // There is nowhere left to report a log that cannot be written.
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Quietly(log.Dispose);
        }

        base.Dispose(disposing);
    }
}
