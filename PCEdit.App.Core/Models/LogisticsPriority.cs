namespace PCEdit.App.Core.Models;

/// <summary>
/// A logistics container's priority as one of the game's 7 named levels. The save stores it as a
/// raw int (<c>Inventory.Priority</c>): -3..3 with 0 = <see cref="Normal"/> (the default the game
/// writes for a freshly placed container). A raw value outside that range is not one of these
/// levels — it is carried through untouched, never coerced.
/// </summary>
public enum LogisticsPriority
{
    Lowest = -3,
    VeryLow = -2,
    Low = -1,
    Normal = 0,
    High = 1,
    VeryHigh = 2,
    Highest = 3,
}

public static class LogisticsPriorityLevels
{
    /// <summary>Every named level, lowest first.</summary>
    public static readonly IReadOnlyList<LogisticsPriority> All =
    [
        LogisticsPriority.Lowest,
        LogisticsPriority.VeryLow,
        LogisticsPriority.Low,
        LogisticsPriority.Normal,
        LogisticsPriority.High,
        LogisticsPriority.VeryHigh,
        LogisticsPriority.Highest,
    ];

    /// <summary>The named level for a raw save value, or null when it is outside -3..3.</summary>
    public static LogisticsPriority? Known(int raw) => raw is >= -3 and <= 3 ? (LogisticsPriority)raw : null;

    public static int ToRaw(this LogisticsPriority priority) => (int)priority;

    /// <summary>The <c>Strings.resx</c> key for a named level.</summary>
    public static string ResourceKey(this LogisticsPriority priority) => priority switch
    {
        LogisticsPriority.Lowest => "Logistics_PriorityLowest",
        LogisticsPriority.VeryLow => "Logistics_PriorityVeryLow",
        LogisticsPriority.Low => "Logistics_PriorityLow",
        LogisticsPriority.Normal => "Logistics_PriorityNormal",
        LogisticsPriority.High => "Logistics_PriorityHigh",
        LogisticsPriority.VeryHigh => "Logistics_PriorityVeryHigh",
        LogisticsPriority.Highest => "Logistics_PriorityHighest",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Not a named priority level."),
    };
}
