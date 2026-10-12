namespace PCEdit.Desktop.Platform;

/// <summary>Where PCEdit keeps its own files on this machine.</summary>
internal static class AppPaths
{
    /// <summary>Set by the UI tests, before anything is resolved, so they never read or write
    /// the user's real settings, backups or log. Null in the shipped app.</summary>
    internal static string? RootOverride { get; set; }

    /// <summary><c>&lt;ApplicationData&gt;/PCEdit/settings.json</c> (e.g. <c>~/.config/PCEdit</c> on Linux).</summary>
    public static string Settings => Path.Combine(Roaming, "PCEdit", "settings.json");

    public static string Backups => Path.Combine(Local, "PCEdit", "backups");

    /// <summary>Machine-local for the same reason as the backups; it also holds file paths.</summary>
    public static string Log => Path.Combine(Local, "PCEdit", "pcedit.log");

    private static string Roaming => RootOverride is { } root
        ? Path.Combine(root, "config")
        : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

    // LocalApplicationData, deliberately NOT the ApplicationData that settings.json uses: on
    // Windows that is the *roaming* profile, and several megabytes of save copies per edited file
    // has no business syncing between machines or counting against a profile quota. settings.json
    // is a few hundred bytes, so roaming it costs nothing and it stays where it is.
    //
    // Resolves to %LocalAppData% on Windows, ~/.local/share (or $XDG_DATA_HOME) on Linux, and
    // ~/Library/Application Support on macOS. That last one moved between .NET 7 and .NET 8, so
    // treat it as a deliberate choice rather than a fixed constant: a future runtime bump could
    // relocate it and make existing backups invisible.
    private static string Local => RootOverride is { } root
        ? Path.Combine(root, "local")
        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
}
