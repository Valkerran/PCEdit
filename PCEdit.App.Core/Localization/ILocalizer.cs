using System.ComponentModel;
using System.Globalization;

namespace PCEdit.App.Core.Localization;

/// <summary>
/// UI string catalog with a live culture switch. Raises <see cref="INotifyPropertyChanged"/>
/// for the indexer (<c>"Item[]"</c>) whenever the culture changes so XAML bindings re-read.
/// </summary>
public interface ILocalizer : INotifyPropertyChanged
{
    /// <summary>Raised after <see cref="SetCulture"/> changes the active culture.</summary>
    event EventHandler? CultureChanged;

    /// <summary>The translated string for <paramref name="key"/>, falling back to English, then <c>"#key"</c>.</summary>
    string this[string key] { get; }

    /// <summary><c>string.Format</c> of the translated string for <paramref name="key"/> with the current culture.</summary>
    string Format(string key, params object?[] args);

    /// <summary>The active UI culture.</summary>
    CultureInfo Current { get; }

    /// <summary>Switches the active UI culture and notifies bindings. Unknown names fall back to English.</summary>
    void SetCulture(string cultureName);

    /// <summary>The 15 supported UI languages, in menu order.</summary>
    IReadOnlyList<LocaleOption> AvailableLocales { get; }
}
