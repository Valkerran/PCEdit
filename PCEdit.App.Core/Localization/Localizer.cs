using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace PCEdit.App.Core.Localization;

/// <inheritdoc />
public sealed class Localizer : ILocalizer
{
    private static readonly ResourceManager Resources =
        new("PCEdit.App.Core.Resources.Strings", typeof(Localizer).Assembly);

    /// <summary>The 15 supported locales, in menu order. <c>en-US</c> is the fallback.</summary>
    public static readonly IReadOnlyList<LocaleOption> SupportedLocales =
    [
        new("en-US", "English (US)", "English (United States)"),
        new("en-GB", "English (UK)", "English (United Kingdom)"),
        new("fr", "Français", "French"),
        new("de", "Deutsch", "German"),
        new("es-ES", "Español (España)", "Spanish (Spain)"),
        new("zh-Hans", "简体中文", "Chinese (Simplified)"),
        new("ru", "Русский", "Russian"),
        new("pl", "Polski", "Polish"),
        new("pt-PT", "Português (Portugal)", "Portuguese (Portugal)"),
        new("ko", "한국어", "Korean"),
        new("ja", "日本語", "Japanese"),
        new("pt-BR", "Português (Brasil)", "Portuguese (Brazil)"),
        new("it", "Italiano", "Italian"),
        new("zh-Hant", "繁體中文", "Chinese (Traditional)"),
        new("tr", "Türkçe", "Turkish"),
    ];

    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(null);
    private static readonly PropertyChangedEventArgs IndexerChanged = new("Item[]");

    private CultureInfo _current = CultureInfo.GetCultureInfo("en-US");

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CultureChanged;

    public CultureInfo Current => _current;

    public IReadOnlyList<LocaleOption> AvailableLocales => SupportedLocales;

    public string this[string key] =>
        Resources.GetString(key, _current)
        ?? Resources.GetString(key, CultureInfo.GetCultureInfo("en-US"))
        ?? $"#{key}";

    public string Format(string key, params object?[] args) =>
        string.Format(_current, this[key], args);

    public void SetCulture(string cultureName)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.GetCultureInfo("en-US");
        }

        if (culture.Name == _current.Name)
        {
            return;
        }

        _current = culture;

        // UI culture only: number/date parsing in the app is pinned to InvariantCulture on purpose.
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Null name = "every property changed": forces all bindings on this source (including the
        // string indexer used by the Translate markup extension) to re-read.
        PropertyChanged?.Invoke(this, AllPropertiesChanged);
        PropertyChanged?.Invoke(this, IndexerChanged);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }
}
