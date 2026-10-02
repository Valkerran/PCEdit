using System.Text.Json;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// File-backed persistence for the UI language, disclaimer acknowledgement and how the Inventories
/// page shows identical items, stored at
/// <c>&lt;ApplicationData&gt;/PCEdit/settings.json</c> (e.g. <c>~/.config/PCEdit</c> on Linux).
/// </summary>
public sealed class JsonSettingsStore : ILanguageStore, IDisclaimerGate, IInventoryDisplayStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
        "PCEdit",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object _gate = new();
    private Settings _settings;

    public JsonSettingsStore() => _settings = Read();

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
            _settings = change(_settings);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Could not write settings: {ex}");
            }
        }
    }

    private static Settings Read()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath)) ?? new Settings()
                : new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    private sealed record Settings
    {
        public string? UiCulture { get; init; }

        public int DisclaimerAckVersion { get; init; }

        public string? StackingMode { get; init; }
    }
}
