namespace PCEdit.Desktop.Platform;

/// <summary>Where PCEdit keeps its own files on this machine.</summary>
internal static class AppPaths
{
    /// <summary><c>&lt;ApplicationData&gt;/PCEdit/settings.json</c> (e.g. <c>~/.config/PCEdit</c> on Linux).</summary>
    public static string Settings { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
        "PCEdit",
        "settings.json");

    // LocalApplicationData, deliberately NOT the ApplicationData that settings.json uses: on
    // Windows that is the *roaming* profile, and several megabytes of save copies per edited file
    // has no business syncing between machines or counting against a profile quota. settings.json
    // is a few hundred bytes, so roaming it costs nothing and it stays where it is.
    //
    // Resolves to %LocalAppData% on Windows, ~/.local/share (or $XDG_DATA_HOME) on Linux, and
    // ~/Library/Application Support on macOS. That last one moved between .NET 7 and .NET 8, so
    // treat it as a deliberate choice rather than a fixed constant: a future runtime bump could
    // relocate it and make existing backups invisible.
    private static readonly string LocalData = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        "PCEdit");

    public static string Backups { get; } = Path.Combine(LocalData, "backups");

    /// <summary>Machine-local for the same reason as the backups; it also holds file paths.</summary>
    public static string Log { get; } = Path.Combine(LocalData, "pcedit.log");
}
