using System.Globalization;

namespace PCEdit.App.Core.Localization;

/// <summary>Resolves which UI culture to start in.</summary>
public static class LanguageStartup
{
    public const string DefaultCulture = "en-US";

    /// <summary>
    /// Startup culture: the saved choice if it is still supported, otherwise the OS UI language
    /// if it maps onto a supported locale, otherwise <see cref="DefaultCulture"/>.
    /// </summary>
    public static string ResolveCulture(ILanguageStore store, IReadOnlyList<LocaleOption> supported)
    {
        var saved = store.GetSavedCulture();
        if (!string.IsNullOrWhiteSpace(saved) && IsSupported(saved, supported, out var exact))
        {
            return exact;
        }

        return MatchOsCulture(CultureInfo.InstalledUICulture, supported) ?? DefaultCulture;
    }

    private static bool IsSupported(string cultureName, IReadOnlyList<LocaleOption> supported, out string match)
    {
        foreach (var locale in supported)
        {
            if (string.Equals(locale.CultureName, cultureName, StringComparison.OrdinalIgnoreCase))
            {
                match = locale.CultureName;
                return true;
            }
        }

        match = DefaultCulture;
        return false;
    }

    private static string? MatchOsCulture(CultureInfo os, IReadOnlyList<LocaleOption> supported)
    {
        // 1. Exact match (walk the parent chain: "en-CA" has no exact entry, "en" would).
        for (var c = os; !string.IsNullOrEmpty(c.Name); c = c.Parent)
        {
            foreach (var locale in supported)
            {
                if (string.Equals(locale.CultureName, c.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return locale.CultureName;
                }
            }
        }

        // 2. Chinese needs the script; match Hans/Hant explicitly.
        if (string.Equals(os.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase))
        {
            var traditional = os.Name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                              || os.Name is "zh-TW" or "zh-HK" or "zh-MO";
            return traditional ? "zh-Hant" : "zh-Hans";
        }

        // 3. Same language, any region (e.g. OS "pt-AO" -> "pt-PT"; "es-MX" -> "es-ES").
        foreach (var locale in supported)
        {
            var localeTwoLetter = CultureInfo.GetCultureInfo(locale.CultureName).TwoLetterISOLanguageName;
            if (string.Equals(localeTwoLetter, os.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
            {
                return locale.CultureName;
            }
        }

        return null;
    }
}
