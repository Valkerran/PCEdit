using System.Text.Json;
using PCEdit.App.Core.Localization;

namespace PCEdit.Desktop.Platform;

/// <summary>
/// File-backed persistence for the UI language and disclaimer acknowledgement, stored at
/// <c>&lt;ApplicationData&gt;/PCEdit/settings.json</c> (e.g. <c>~/.config/PCEdit</c> on Linux).
/// </summary>
public sealed class JsonSettingsStore : ILanguageStore, IDisclaimerGate
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
                System.Diagnostics.Debug.WriteLine($"Could not write settings: {ex}");
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
    }
}
