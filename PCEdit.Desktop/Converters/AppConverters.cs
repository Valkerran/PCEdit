using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

/// <summary>Two-way match between any enum value and a member name (ConverterParameter), for
/// binding a group of RadioButtons to one enum property - the Inventories type filter and the
/// identical-items setting both use it.</summary>
public sealed class EnumIsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum member && parameter is string name &&
        string.Equals(member.ToString(), name, StringComparison.OrdinalIgnoreCase);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string name && targetType.IsEnum
            && Enum.TryParse(targetType, name, ignoreCase: true, out var member)
            ? member
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

/// <summary>
/// True when a vital gauge value is at <see cref="Level"/> (ConverterParameter <c>HighIsBad</c> for
/// toxicity), for binding a style class. The colour itself comes from a style with a
/// DynamicResource, so it follows a live light/dark switch - a converter that resolved the brush
/// once kept the old theme's colour, leaving vitals near-invisible after switching to dark.
/// </summary>
public sealed class VitalLevelIsConverter : IValueConverter
{
    public VitalLevel Level { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        VitalStatus.Classify(value, parameter) == Level;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True when a status message is of <see cref="Kind"/>, for binding a style class (see
/// <see cref="VitalLevelIsConverter"/> for why it is not a colour).</summary>
public sealed class StatusKindIsConverter : IValueConverter
{
    public StatusKind Kind { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is StatusKind kind && kind == Kind;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
