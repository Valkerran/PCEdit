using System.Text.Json;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler;

namespace PCEdit.App.Core.Services;

/// <summary>
/// File-backed persistence for the UI language, disclaimer acknowledgement and how the Inventories
/// page shows identical items. The UI head says where the file lives.
/// </summary>
public sealed class JsonSettingsStore : ILanguageStore, IDisclaimerGate, IInventoryDisplayStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object _gate = new();
    private readonly string _path;
    private Settings _settings;

    public JsonSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _path = Path.GetFullPath(settingsPath);
        _settings = ReadOr(new Settings());
    }

    public string? GetSavedCulture() =>
        string.IsNullOrWhiteSpace(_settings.UiCulture) ? null : _settings.UiCulture;

    public void SaveCulture(string cultureName) =>
        Mutate(s => s with { UiCulture = cultureName });

    public bool HasAcknowledged => _settings.DisclaimerAckVersion >= DisclaimerMeta.Version;

    public void Acknowledge() =>
        Mutate(s => s with { DisclaimerAckVersion = DisclaimerMeta.Version });

    // Stored by name. The file is hand-editable, so anything that is not a known mode - a typo, a
    // number, a value from a newer version - falls back to the default rather than failing.
    public StackingMode GetStackingMode() =>
        Enum.GetValues<StackingMode>()
            .FirstOrDefault(mode => string.Equals(mode.ToString(), _settings.StackingMode, StringComparison.OrdinalIgnoreCase));

    public void SaveStackingMode(StackingMode mode) =>
        Mutate(s => s with { StackingMode = mode.ToString() });

    private void Mutate(Func<Settings, Settings> change)
    {
        lock (_gate)
        {
            // Start from the file, not from what this window read at startup: another PCEdit
            // window may have saved a different setting since (issue #100). Two windows writing
            // in the same instant can still race; that is not worth a cross-process lock.
            _settings = change(ReadOr(_settings));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                AtomicFile.WriteAllBytes(_path, JsonSerializer.SerializeToUtf8Bytes(_settings, JsonOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Could not write settings: {ex}");
            }
        }
    }

    /// <summary>The settings on disk, or <paramref name="fallback"/> when there is no file or it
    /// cannot be read - the file is hand-editable, and a broken one must not stop the app. Read as
    /// text, which drops the byte-order mark some editors add; the JSON reader refuses one.</summary>
    private Settings ReadOr(Settings fallback)
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) ?? fallback
                : fallback;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return fallback;
        }
    }

    private sealed record Settings
    {
        public string? UiCulture { get; init; }

        public int DisclaimerAckVersion { get; init; }

        public string? StackingMode { get; init; }
    }
}
