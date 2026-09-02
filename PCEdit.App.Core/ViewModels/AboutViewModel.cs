using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class AboutViewModel(
    ILocalizer localizer,
    ILanguageStore languageStore,
    IAppVersionInfo appInfo,
    ISaveBackupService backups) : ObservableObject, ILoadable
{
    private readonly ILocalizer _localizer = localizer;
    private readonly ILanguageStore _languageStore = languageStore;
    private readonly IAppVersionInfo _appInfo = appInfo;
    private readonly ISaveBackupService _backups = backups;

    public IReadOnlyList<LocaleOption> Languages => _localizer.AvailableLocales;

    public string VersionText => _localizer.Format(LocKeys.About_Version, _appInfo.Version);

    /// <summary>
    /// Where PCEdit keeps its own copies of a save from before it first wrote to one. Shown
    /// because a backup nobody can find is not a safety net - and on macOS this lives under
    /// ~/Library, which Finder hides by default.
    /// </summary>
    public string BackupsPath => _backups.DirectoryPath;

    /// <summary>The game's default save-folder per platform. Kept in step with the README's
    /// "Game Save Locations" section.</summary>
    public IReadOnlyList<SaveLocationInfo> SaveLocations { get; } =
    [
        new("Windows · Steam", @"%UserProfile%\AppData\LocalLow\MijuGames\Planet Crafter"),
        new("Windows · Xbox", @"%LocalAppData%\Packages\MijuGames.ThePlanetCrafter_ta6nvwnbx9v7t\SystemAppData\wgs"),
        new("Linux · Steam (Proton)", "~/.steam/steam/steamapps/compatdata/1284190/pfx/drive_c/users/steamuser/AppData/LocalLow/MijuGames/Planet Crafter/"),
        new("macOS", "~/Library/Application Support/MijuGames/Planet Crafter"),
    ];

    [ObservableProperty]
    private LocaleOption? _selectedLanguage;

    public void Load()
    {
        // "Version {0}" is localized; the shell forces a reload on language change.
        OnPropertyChanged(nameof(VersionText));

        SelectedLanguage =
            Languages.FirstOrDefault(l => string.Equals(l.CultureName, _localizer.Current.Name, StringComparison.OrdinalIgnoreCase))
            ?? Languages.FirstOrDefault(l =>
                string.Equals(
                    CultureInfo.GetCultureInfo(l.CultureName).TwoLetterISOLanguageName,
                    _localizer.Current.TwoLetterISOLanguageName,
                    StringComparison.OrdinalIgnoreCase));
    }

    partial void OnSelectedLanguageChanged(LocaleOption? value)
    {
        if (value is null || string.Equals(value.CultureName, _localizer.Current.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _localizer.SetCulture(value.CultureName);
        _languageStore.SaveCulture(value.CultureName);
        OnPropertyChanged(nameof(VersionText));
    }
}
