using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using PCEdit.App.Core.Localization;

namespace PCEdit.Desktop.Markup;

/// <summary>
/// <c>{m:Loc Some_Key}</c> — binds to the active <see cref="ILocalizer"/> so text re-reads when
/// the language changes.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    public string Key { get; set; } = string.Empty;

    // Bind to Current (a plain property that changes on SetCulture) rather than the string indexer:
    // Avalonia's reflection binding does not re-read an indexer path on an "Item[]" notification, so
    // a direct [Key] binding would never refresh when the language changes.
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding
    {
        Path = nameof(ILocalizer.Current),
        Source = Loc.Instance,
        Mode = BindingMode.OneWay,
        Converter = new LocConverter(Key),
    };
}

internal sealed class LocConverter(string key) : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Loc.Instance[key];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// <c>{m:LocFormat Key=Some_Key, Path=SomeProperty}</c> — formats the translated string for
/// <c>Key</c> with one argument bound from <c>Path</c>. <c>FallbackKey</c> supplies the text when
/// the argument is null/empty.
/// </summary>
public sealed class LocFormatExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? FallbackKey { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var multi = new MultiBinding
        {
            Mode = BindingMode.OneWay,
            Converter = new LocFormatConverter(Key, FallbackKey),
        };
        multi.Bindings.Add(new Binding("Current") { Source = Loc.Instance });
        multi.Bindings.Add(new Binding(Path));
        return multi;
    }
}

internal sealed class LocFormatConverter(string key, string? fallbackKey) : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var arg = values.Count > 1 ? values[1] : null;

        if (fallbackKey is not null && (arg is null || (arg is string s && s.Length == 0)))
        {
            return Loc.Instance[fallbackKey];
        }

        return Loc.Instance.Format(key, arg);
    }
}
