using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Presentation;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.Desktop.Converters;

/// <summary>Singletons for XAML <c>{x:Static}</c> use.</summary>
public static class AppConverters
{
    public static readonly IValueConverter InvertedBool = new InvertedBoolConverter();
    public static readonly IValueConverter StringNotEmpty = new StringNotEmptyConverter();
    public static readonly IValueConverter DirtyStateText = new DirtyStateTextConverter();
    public static readonly IValueConverter VitalText = new VitalStatusTextConverter();
    public static readonly IValueConverter VitalBrush = new VitalStatusColorConverter();
    public static readonly IValueConverter StatusBrush = new StatusKindToColorConverter();
    public static readonly IValueConverter Icon = new IconPathConverter();
}

public sealed class CountConverter : IValueConverter
{
    /// <summary>When true, returns <c>true</c> for a zero count; otherwise <c>true</c> for a positive count.</summary>
    public bool Zero { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value is int i ? i : 0;
        return Zero ? count == 0 : count > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Resolves an <c>ItemCatalog</c> icon file name (e.g. <c>cat_ore.png</c>) to a bundled bitmap.</summary>
public sealed class IconPathConverter : IValueConverter
{
    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.Ordinal);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string file || string.IsNullOrEmpty(file))
        {
            return null;
        }

        if (!Cache.TryGetValue(file, out var bitmap))
        {
            try
            {
                var uri = new Uri($"avares://PCEdit/Assets/Icons/{file}");
                using var stream = AssetLoader.Open(uri);

                // Source PNGs are 256px; the list renders them at ~28px. Decoding straight to a
                // small size is far cheaper to decode, scale and hold than a full-res bitmap.
                bitmap = Bitmap.DecodeToWidth(stream, 64, BitmapInterpolationMode.HighQuality);
            }
            catch
            {
                bitmap = null;
            }

            Cache[file] = bitmap;
        }

        return bitmap;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Two-way match between an <see cref="InventoryFilter"/> and a name (ConverterParameter),
/// for binding a group of RadioButtons to the single filter property.</summary>
public sealed class InventoryFilterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is InventoryFilter f && parameter is string s &&
        string.Equals(f.ToString(), s, StringComparison.OrdinalIgnoreCase);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string s && Enum.TryParse<InventoryFilter>(s, ignoreCase: true, out var f)
            ? f
            : BindingOperations.DoNothing;
}

public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}

public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class DirtyStateTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Loc.Instance[value is true ? LocKeys.Dirty_Unsaved : LocKeys.Dirty_Saved];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class VitalStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Loc.Instance[VitalStatus.Classify(value, parameter) switch
        {
            VitalLevel.Critical => LocKeys.Vital_Critical,
            VitalLevel.Low => LocKeys.Vital_Low,
            _ => LocKeys.Vital_Ok,
        }];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class VitalStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ThemeBrush.Resolve(StatusPalette.KeyFor(VitalStatus.Classify(value, parameter)));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StatusKindToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ThemeBrush.Resolve(StatusPalette.KeyFor(value as StatusKind? ?? StatusKind.Info));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal static class ThemeBrush
{
    public static IBrush Resolve(string key)
    {
        var app = Application.Current;
        var theme = app?.ActualThemeVariant ?? ThemeVariant.Default;
        if (app is not null && app.TryGetResource(key, theme, out var res) && res is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }
}
