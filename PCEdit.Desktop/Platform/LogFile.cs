using System.Diagnostics;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// Sends <see cref="Trace"/> output to a file. A release build has no other listener, so without
/// this every diagnostic line the app writes goes nowhere (issue #74).
/// </summary>
internal static class LogFile
{
    // Roughly: the limit is applied at start-up in bytes and during a session in characters.
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

            // Shared for writing, so a second PCEdit window can log to the same file.
            var file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            Trace.Listeners.Add(new QuietTraceListener(new StreamWriter(file), MaxBytes));
            Trace.WriteLine($"{DateTime.UtcNow:O} PCEdit started.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A log that cannot be opened is no reason not to start.
        }
    }
}
