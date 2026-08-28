using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class AboutViewModel(
    ILocalizer localizer,
    ILanguageStore languageStore,
    IAppVersionInfo appInfo) : ObservableObject, ILoadable
{
    private readonly ILocalizer _localizer = localizer;
    private readonly ILanguageStore _languageStore = languageStore;
    private readonly IAppVersionInfo _appInfo = appInfo;

    public IReadOnlyList<LocaleOption> Languages => _localizer.AvailableLocales;

    public string VersionText => _localizer.Format(LocKeys.About_Version, _appInfo.Version);

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
