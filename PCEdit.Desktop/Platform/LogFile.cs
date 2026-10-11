using System.Diagnostics;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// Sends <see cref="Trace"/> output to a file. A release build has no other listener, so without
/// this every diagnostic line the app writes goes nowhere (issue #74).
/// </summary>
internal static class LogFile
{
    private const long MaxBytes = 1024 * 1024;

    public static void Start(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // ponytail: one previous file kept, no rolling by date; add that if anyone needs
            // more history than the last megabyte.
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
            {
                File.Move(path, path + ".old", overwrite: true);
            }

            Trace.Listeners.Add(new TextWriterTraceListener(path));
            Trace.AutoFlush = true;
            Trace.WriteLine($"{DateTime.UtcNow:O} PCEdit started.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A log that cannot be opened is no reason not to start.
        }
    }
}
